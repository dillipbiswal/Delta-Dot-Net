using System;
using System.ServiceModel.Activation;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Azure.Queue;
using Datavail.Delta.Infrastructure.Azure.QueueMessage;
using Datavail.Delta.Infrastructure.Logging;
using Microsoft.Practices.Unity.Utility;

namespace Datavail.Delta.WebServices
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class CheckInService : ICheckInService
    {
        private readonly IAzureQueue<CheckInMessage> _checkInQueue;
        private readonly IServerService _serverService;
        private readonly IDeltaLogger _logger;

        public CheckInService(IAzureQueue<CheckInMessage> checkInQueue, IDeltaLogger deltaLogger, IServerService serverService)
        {
            _checkInQueue = checkInQueue;
            _serverService = serverService;
            _logger = deltaLogger;
        }

        public bool CheckIn(DateTime timestamp, Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion)
        {
            try
            {
                _logger.LogDebug(string.Format("CheckIn called with: timestamp={0}, tenantId={1}, serverId={2}, hostname={3}, ipAddress={4}, agentVersion={5}", timestamp, tenantId, serverId, hostname, ipAddress, agentVersion));

                Guard.ArgumentNotNull(tenantId, "TenantId");
                Guard.ArgumentNotNull(serverId, "ServerId");
                Guard.ArgumentNotNullOrEmpty(hostname, "Hostname");
                Guard.ArgumentNotNullOrEmpty(ipAddress, "IpAddress");

                var msg = new CheckInMessage() { Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp };

                _checkInQueue.AddMessage(msg);
                _serverService.CheckIn(tenantId, serverId, hostname, ipAddress, agentVersion);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(ex.Message, ex);
                return false;
            }
        }
    }
}