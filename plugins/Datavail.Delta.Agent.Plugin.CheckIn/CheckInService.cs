using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Logging;
using RestSharp;
using System;
using System.Configuration;
using System.Net;
using System.Threading;

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
                var client = new RestClient(ConfigurationManager.AppSettings["DeltaApiUrl"]);
                var request = new RestRequest("Server/CheckIn/{id}", Method.POST);

                //Add ServerId to the URL
                request.AddUrlSegment("id", serverId.ToString());

                //Create JSON body
                request.AddParameter("Hostname", hostname);
                request.AddParameter("IpAddress", ipAddress);
                request.AddParameter("Timestamp", DateTime.UtcNow);
                request.AddParameter("TenantId", tenantId.ToString());
                request.AddParameter("AgentVersion", agentVersion);
                request.AddParameter("CustomerId", customerId.ToString());

                var response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var errorMessage = string.Format("Error while checking in. {0}: {1}", response.StatusCode, response.ErrorMessage);
                    _logger.LogSpecificError(WellKnownAgentMesage.UnhandledException, errorMessage);

                    DoBackOff();
                }
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
