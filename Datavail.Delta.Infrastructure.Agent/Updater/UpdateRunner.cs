using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;

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
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
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
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
            }
        }

        private void GetNewConfigFile()
        {
            try
            {
                _logger.LogDebug("Checking for updated configuration");
                using (var updateSvc = new UpdateServiceProxy.UpdateServiceClient())
                {
                    var serverId = _common.GetServerId();
                    var config = updateSvc.GetConfig(serverId);

                    if (File.Exists(_newConfigFile))
                        File.Delete(_newConfigFile);

                    if (config != null && config != GetCurrentConfig())
                    {
                        File.WriteAllText(_newConfigFile, config);
                        _logger.LogDebug("New configuration file found");
                    }

                    updateSvc.Close();
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
                using (var updateSvc = new UpdateServiceProxy.UpdateServiceClient())
                {
                    var serverId = _common.GetServerId();
                    var assemblyList = updateSvc.GetAssemblyList(serverId);

                    if (assemblyList != null)
                    {
                        _logger.LogDebug("Found " + assemblyList.Count + " required assemblies");

                        foreach (var key in assemblyList.Keys)
                        {
                            var filename = Path.Combine(_common.GetPluginPath(), key + "." + assemblyList[key] + ".dll");
                            if (!File.Exists(filename))
                            {
                                _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, string.Format("Downloading {0}.{1}", key, assemblyList[key]));
                                var tempfilename = Path.Combine(_common.GetTempPath(), key + "." + assemblyList[key] + ".dll");
                                var fileContents = updateSvc.GetAssembly(serverId, key, assemblyList[key]);

                                if (File.Exists(tempfilename))
                                    File.Delete(tempfilename);

                                File.WriteAllBytes(tempfilename, fileContents);
                            }
                            else
                            {
                                _logger.LogDebug("Skipping " + assemblyList[key] + ", file already exists locally");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Found 0 required assemblies");
                    }

                    updateSvc.Close();
                    _common.SetBackoffTimer(0);
                }
            }
            catch (EndpointNotFoundException)
            {
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Update WebService is unreachable");
                DoBackOff();
            }
            catch (CommunicationException)
            {
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Update WebService is unreachable");
                DoBackOff();
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