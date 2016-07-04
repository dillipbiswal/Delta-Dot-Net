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

namespace Datavail.Delta.Infrastructure.Agent.OnDemandUpdater
{
    public class OnDemandUpdateRunner
    {
        private static string _newConfigFile;
        private static string _oldConfigFile;
        private readonly ICommon _common;
        private readonly IDeltaLogger _logger = new DeltaLogger();
        private static readonly EventWaitHandle SchedulerWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public OnDemandUpdateRunner(ICommon common)
        {
            _common = common;
            Init();
        }

        public OnDemandUpdateRunner()
        {
            _common = new Common.Common();
            Init();
        }

        private void Init()
        {
            _newConfigFile = Path.Combine(_common.GetTempPath(), "DeltaOnDemandMetric.xml");
            _oldConfigFile = _common.GetOnDemandConfigPath();
        }

        public void Execute()
        {
            try
            {
                var runInterval = _common.GetUpdaterRunInterval();
                var start = DateTime.UtcNow;

                GetNewConfigFile();

                if (File.Exists(_newConfigFile))
                {

                    StopAgentService();

                    try
                    {
                        if ((!File.Exists(_oldConfigFile)) && File.Exists(_newConfigFile))
                        {
                            //File.Delete(_oldConfigFile);

                            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Installing new configuration to " + _oldConfigFile);
                            File.Move(_newConfigFile, _oldConfigFile);

                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Unhandled Exception in OnDemandUpdateRunner", ex);
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
                _logger.LogUnhandledException("Unhandled Exception in OnDemandUpdateRunner", ex);
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
                _logger.LogDebug("Checking for updated OnDemand configuration");

                var client = new RestClient(ConfigurationManager.AppSettings["DeltaApiUrl"]);
                var request = new RestRequest("Server/OnDemandConfig/{id}", Method.GET);

                //Add ServerId to the URL
                var serverId = _common.GetServerId();
                request.AddUrlSegment("id", serverId.ToString());

                var response = client.Execute<ConfigModel>(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //_logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "config :" + response.Data.Configuration + ":" + response.Data.Configuration.ToString().Length + ":");
                    if (response.Data.Configuration.ToString().Length > 0)
                    {
                        if (File.Exists(_newConfigFile))
                            File.Delete(_newConfigFile);

                        if (!File.Exists(_oldConfigFile))
                        {
                            File.WriteAllText(_newConfigFile, response.Data.Configuration);
                            _logger.LogDebug("New configuration file found [" + response.Data.GeneratingServer + " at " + response.Data.Timestamp + "]");
                        }
                        //else
                        //{
                        //    if (response.Data.Configuration != null && response.Data.Configuration != GetCurrentConfig())
                        //    {
                        //        File.WriteAllText(_newConfigFile, response.Data.Configuration);
                        //        _logger.LogDebug("New configuration file found [" + response.Data.GeneratingServer + " at " + response.Data.Timestamp + "]");
                        //    }
                        //    else
                        //    {
                        //        _logger.LogDebug("OnDemand Agent Configuration does not need to be updated");
                        //    }
                        //}
                    }
                }
                else
                {
                    _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, string.Format("Response Status {0}: {1} ", response.StatusCode, response.ErrorMessage));
                }

                _common.SetBackoffTimer(0);
            }
            catch (EndpointNotFoundException)
            {
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The OnDemand Update WebService is unreachable");
                DoBackOff();
            }
            catch (CommunicationException ex)
            {
                _logger.LogDebug(ex.Message);
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The OnDemand Update WebService is unreachable");
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