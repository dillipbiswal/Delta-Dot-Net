using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Models;
using Newtonsoft.Json;
using RestSharp;

namespace Datavail.Delta.Infrastructure.Agent.Updater
{
    public class UpdateRunner
    {
        private static string _newConfigFile;
        private static string _oldConfigFile;
        private readonly ICommon _common;
        private readonly IDeltaLogger _logger = new DeltaLogger();

        public UpdateRunner(ICommon common)
        {
            _common = common;
            Init();
        }

        public UpdateRunner()
        {
            _common = new Common.Common();
            Init();
        }

        private void Init()
        {
            _newConfigFile = Path.Combine(_common.GetTempPath(), "DeltaAgent.xml");
            _oldConfigFile = _common.GetConfigPath();
        }

        public void Execute()
        {
            try
            {
                var runInterval = _common.GetUpdaterRunInterval();
                var start = DateTime.UtcNow;

                GetNewConfigFile();
                GetRequiredAssemblies();

                if (File.Exists(_newConfigFile) || Directory.GetFiles(_common.GetTempPath(), "*.dll").Any())
                {
                    StopAgentService();

                    try
                    {
                        if (File.Exists(_oldConfigFile) && File.Exists(_newConfigFile))
                        {
                            File.Delete(_oldConfigFile);

                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Installing new configuration to " + _oldConfigFile);
                            File.Move(_newConfigFile, _oldConfigFile);
                        }

                        var dlls = Directory.GetFiles(_common.GetTempPath(), "*.dll");
                        var plugins = Directory.GetFiles(_common.GetPluginPath(), "*.dll");

                        foreach (var dll in dlls)
                        {
                            var newFile = dll.Replace(_common.GetTempPath(), _common.GetPluginPath());
                            _logger.LogDebug("Moving " + dll + " to " + newFile);
                            File.Move(dll, newFile);

                            //strip off the version and dll
                            var newfileNoExt = newFile.Substring(0, newFile.Length - 16);
                            foreach (var plugin in plugins)
                            {
                                var oldFile = plugin.Substring(0, plugin.Length - 16);

                                if (oldFile == newfileNoExt)
                                {
                                    _logger.LogDebug("Deleting old plugin " + plugin);
                                    File.Delete(plugin);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Unhandled Exception in UpdateRunner", ex);
                    }
                    finally
                    {
                        StartAgentService();
                    }
                }

                var sleep = DateTime.UtcNow.Subtract(start).Seconds;
                if (sleep < runInterval)
                {
                    _logger.LogDebug("Sleeping for " + (runInterval - sleep).ToString(CultureInfo.InvariantCulture) + " seconds");
                    Thread.Sleep(TimeSpan.FromSeconds(runInterval - sleep));
                }
            }
            catch (ThreadAbortException)
            {
                //Don't log anything if we're shutting down
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in UpdateRunner", ex);
            }
        }

        private void StopAgentService()
        {
            _logger.LogDebug("Stopping Delta Agent");
            if (OsInfo.IsRunningOnUnix())
            {
                var proc = new System.Diagnostics.Process
                               {
                                   EnableRaisingEvents = false,
                                   StartInfo =
                                       {
                                           FileName = ConfigurationManager.AppSettings["initScript"],
                                           Arguments = "stop"
                                       }
                               };
                proc.Start();
                proc.WaitForExit();

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            else
            {
                var service = new ServiceController("Datavail Delta Agent");

                if (service.CanStop)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
                }
            }
        }

        private void StartAgentService()
        {
            _logger.LogDebug("Starting Delta Agent");
            if (OsInfo.IsRunningOnUnix())
            {
                //Give time for service to fully exit
                Thread.Sleep(TimeSpan.FromSeconds(5));

                var proc = new System.Diagnostics.Process
                               {
                                   EnableRaisingEvents = false,
                                   StartInfo =
                                       {
                                           FileName = ConfigurationManager.AppSettings["initScript"],
                                           Arguments = "start"
                                       }
                               };
                proc.Start();
                proc.WaitForExit();

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            else
            {

                Thread.Sleep(TimeSpan.FromSeconds(5));
                var service = new ServiceController("Datavail Delta Agent");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
            }
        }

        private void GetNewConfigFile()
        {
            try
            {
                _logger.LogDebug("Checking for updated configuration");

                var client = new RestClient(ConfigurationManager.AppSettings["DeltaApiUrl"]);
                var request = new RestRequest("Server/Config/{id}", Method.GET);

                //Add ServerId to the URL
                var serverId = _common.GetServerId();
                request.AddUrlSegment("id", serverId.ToString());

                var response = client.Execute<ConfigModel>(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (File.Exists(_newConfigFile))
                        File.Delete(_newConfigFile);

                    if (response.Data.Configuration != null && response.Data.Configuration != GetCurrentConfig())
                    {
                        File.WriteAllText(_newConfigFile, response.Data.Configuration);
                        _logger.LogDebug("New configuration file found [" + response.Data.GeneratingServer + " at " + response.Data.Timestamp + "]");
                    }
                    else
                    {
                        _logger.LogDebug("Agent Configuration does not need to be updated");
                    }
                }
                else
                {
                    _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, string.Format("Response Status {0}: {1}", response.StatusCode, response.ErrorMessage));
                }

                _common.SetBackoffTimer(0);
            }
            catch (EndpointNotFoundException)
            {
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Update WebService is unreachable");
                DoBackOff();
            }
            catch (CommunicationException ex)
            {
                _logger.LogDebug(ex.Message);
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Update WebService is unreachable");
                DoBackOff();
            }
            catch (ThreadAbortException)
            {
                //Don't log anything if we're shutting down
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in GetNewConfigFile()", ex);
                DoBackOff();
            }
        }

        private static string GetCurrentConfig()
        {
            var currentConfig = string.Empty;

            if (File.Exists(_oldConfigFile))
                currentConfig = File.ReadAllText(_oldConfigFile);

            return currentConfig;
        }

        private void GetRequiredAssemblies()
        {
            try
            {
                _logger.LogDebug("Checking for required assemblies");

                var client = new RestClient(ConfigurationManager.AppSettings["DeltaApiUrl"]);
                var request = new RestRequest("Server/AssemblyList/{id}", Method.GET);

                //Add ServerId to the URL
                var serverId = _common.GetServerId();
                request.AddUrlSegment("id", serverId.ToString());

                var response = client.Execute<AssemblyListModel>(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {

                    var assemblyList = response.Data.Assemblies;

                    if (assemblyList != null)
                    {
                        _logger.LogDebug("Found " + assemblyList.Count + " required assemblies");

                        foreach (var assembly in assemblyList)
                        {
                            var filename = Path.Combine(_common.GetPluginPath(), assembly.AssemblyName + "." + assembly.Version + ".dll");
                            if (!File.Exists(filename))
                            {
                                _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, string.Format("Downloading {0}.{1}", assembly.AssemblyName, assembly.Version));
                                var tempfilename = Path.Combine(_common.GetTempPath(), assembly.AssemblyName + "." + assembly.Version + ".dll");
                                //var fileContents = updateSvc.GetAssembly(serverId, key, assemblyList[key]);

                                var downloadRequest = new RestRequest("Assembly/{name}/{version}", Method.GET);
                                downloadRequest.AddUrlSegment("name", assembly.AssemblyName);
                                downloadRequest.AddUrlSegment("version", assembly.Version);

                                var downloadResponse = client.Execute(downloadRequest);
                                var responseData = JsonConvert.DeserializeObject<AssemblyDownloadModel>(downloadResponse.Content);

                                if (downloadResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    _logger.LogDebug(string.Format("Downloaded {0}.{1}.dll sucessfully", assembly.AssemblyName, assembly.Version));
                                    var fileContents = responseData.Contents;

                                    if (File.Exists(tempfilename))
                                        File.Delete(tempfilename);

                                    File.WriteAllBytes(tempfilename, fileContents);
                                }
                                else
                                {
                                    var errorMessage = string.Format("Error while downloading {0}.{1}.dll. {2}: {3}", assembly.AssemblyName, assembly.Version, response.StatusCode, response.ErrorMessage);
                                    _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, errorMessage);
                                }
                            }
                            else
                            {
                                _logger.LogDebug(string.Format("Skipping {0}.{1}.dll, file already exists locally", assembly.AssemblyName, assembly.Version));
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Found 0 required assemblies");
                    }

                    _common.SetBackoffTimer(0);
                }
            }
            catch (ThreadAbortException)
            {
                //Don't log anything if we're shutting down
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in GetRequiredAssemblies()", ex);
                DoBackOff();
            }
        }

        private void DoBackOff()
        {
            //If there is an error communicating with the service, don't fill the error logs.
            //backoff 30 seconds for each loop until we've reached 10 minutes. Then reset to 1 minute. 
            var backoffTimer = _common.GetBackoffTimer();

            if (backoffTimer >= 600)
            {
                backoffTimer = 60;
            }
            else
            {
                backoffTimer = backoffTimer + 30;
            }

            _common.SetBackoffTimer(backoffTimer);
            _logger.LogSpecificError(WellKnownAgentMesage.InformationalMessage, "Waiting " + backoffTimer + " seconds before retrying communications");

            Thread.Sleep(TimeSpan.FromSeconds(backoffTimer));
        }
    }
}