using System;
using System.ServiceModel;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Microsoft.Practices.Unity.Utility;

namespace Datavail.Delta.Cloud.Ws
{
    public class CheckInService : ICheckInService
    {
        private readonly IQueue<CheckInMessage> _checkInQueue;
        private readonly IServerService _serverService;
        private readonly IDeltaLogger _logger;

        public CheckInService(IQueue<CheckInMessage> checkInQueue, IDeltaLogger deltaLogger, IServerService serverService)
        {
            _checkInQueue = checkInQueue;
            _serverService = serverService;
            _logger = deltaLogger;
        }

        public bool CheckIn(DateTime timestamp, Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion, Guid? customerId = null)
        {
            try
            {
                Guard.ArgumentNotNull(tenantId, "TenantId");
                Guard.ArgumentNotNull(serverId, "ServerId");
                Guard.ArgumentNotNullOrEmpty(hostname, "Hostname");
                Guard.ArgumentNotNullOrEmpty(ipAddress, "IpAddress");

                var msg = new CheckInMessage { Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp };

                _checkInQueue.AddMessage(msg);
                _serverService.CheckIn(tenantId, serverId, hostname, ipAddress, agentVersion, customerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("CheckIn ({0},{1},{2},{3},{4},{5}", timestamp, tenantId, serverId, hostname, ipAddress, agentVersion), ex);
                return false;
            }
        }
    }
}