using System;
using System.ServiceModel;
using System.Threading;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Logging;

namespace Datavail.Delta.Agent.Plugin.CheckIn
{
    internal class CheckInService : ICheckInService
    {
        private readonly IDeltaLogger _logger;
        private int _backoffTimer;

        public CheckInService(IDeltaLogger logger)
        {
            _logger = logger;
        }

        public CheckInService()
        {
            _logger = new DeltaLogger();
        }
        public void CheckIn(Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion, Guid? customerId = null)
        {
            try
            {
                using (var proxy = new CheckInServiceProxy.CheckInServiceClient())
                {
                    proxy.CheckIn(DateTime.UtcNow, tenantId, serverId, hostname, ipAddress, agentVersion, customerId);
                    proxy.Close();
                }
            }
            catch (EndpointNotFoundException)
            {
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Check-In WebService is unreachable");
                DoBackOff();
            }
            catch (CommunicationException)
            {
                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Check-In WebService is unreachable");
                DoBackOff();
            }
            catch (ThreadAbortException)
            {
                //Don't log anything if we're shutting down
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in check-in", ex);
                DoBackOff();
            }
        }

        private void DoBackOff()
        {
            //If there is an error communicating with the service, don't fill the error logs.
            //backoff 30 seconds for each loop until we've reached 10 minutes. Then reset to 1 minute. 
            if (_backoffTimer >= 600)
            {
                _backoffTimer = 60;
            }
            else
            {
                _backoffTimer = _backoffTimer + 30;
            }

            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Waiting " + _backoffTimer + " seconds before retrying check-in");
            Thread.Sleep(TimeSpan.FromSeconds(_backoffTimer));
        }
    }
}
