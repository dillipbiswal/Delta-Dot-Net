using System;
using System.ServiceModel.Activation;
using Datavail.Delta.Infrastructure.Azure.Queue;
using Datavail.Delta.Infrastructure.Azure.QueueMessage;
using Datavail.Delta.Infrastructure.Logging;

namespace Datavail.Delta.WebServices
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class CollectionService : ICollectionService
    {
        private readonly IAzureQueue<DataCollectionArchiveMessage> _archiveQueue;
        private readonly IAzureQueue<DataCollectionMessage> _incidentProcessorQueue;
        private readonly IDeltaLogger _logger;

        public CollectionService(IAzureQueue<DataCollectionArchiveMessage> archiveQueue, IAzureQueue<DataCollectionMessage> incidentProcessorQueue, IDeltaLogger deltaLogger)
        {
            _archiveQueue = archiveQueue;
            _incidentProcessorQueue = incidentProcessorQueue;
            _logger = deltaLogger;
        }

        public bool PostCollection(DateTime timestamp, Guid tenantId, Guid serverId, string hostname, string ipAddress, string data)
        {
            try
            {
                _logger.LogDebug(string.Format("PostCollection called with: timestamp={0}, tenantId={1}, serverId={2}, hostname={3}, ipAddress={4}, data={5}", timestamp, tenantId, serverId, hostname, ipAddress, data));

                var msg = new DataCollectionMessage { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp};
                _incidentProcessorQueue.AddMessage(msg);

                var archivemsg = new DataCollectionArchiveMessage { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp };
                _archiveQueue.AddMessage(archivemsg);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
                return false;
            }
        }
    }
}
