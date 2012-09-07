using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Cloud.Ws
{
    public class CollectionService : ICollectionService
    {
        private readonly IQueue<DataCollectionArchiveMessage> _archiveQueue;
        private readonly IQueue<DataCollectionMessage> _incidentProcessorQueue;
        private readonly IQueue<DataCollectionTestMessage> _testQueue;
        private readonly IDeltaLogger _logger;

        public CollectionService(IQueue<DataCollectionArchiveMessage> archiveQueue, IQueue<DataCollectionMessage> incidentProcessorQueue, IQueue<DataCollectionTestMessage> testQueue, IDeltaLogger deltaLogger)
        {
            _archiveQueue = archiveQueue;
            _incidentProcessorQueue = incidentProcessorQueue;
            _testQueue = testQueue;
            _logger = deltaLogger;
        }

        public bool PostCollection(DateTime timestamp, Guid tenantId, Guid serverId, string hostname, string ipAddress, string data)
        {
            try
            {
                Guard.IsNotNull(data, "Data cannot be null");

                var ignoreExpression = WebConfigurationManager.AppSettings["PluginDataIgnoreExpression"] ?? "^a";
                var testDivertExpression = WebConfigurationManager.AppSettings["PluginDataDivertToTestQueueExpression"] ?? "^a";
                
                if (!Regex.IsMatch(data, ignoreExpression))
                {
                    var msg = new DataCollectionMessage { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp };
                    _incidentProcessorQueue.AddMessage(msg);

                    var archivemsg = new DataCollectionArchiveMessage { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp };
                    _archiveQueue.AddMessage(archivemsg);

                }
                
                if (Regex.IsMatch(data, testDivertExpression))
                {
                    var testmsg = new DataCollectionTestMessage { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId, TenantId = tenantId, Timestamp = timestamp };
                    _testQueue.AddMessage(testmsg);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("Error in PostCollection({0}|{1}|{2}|{3})", tenantId, serverId, hostname, ipAddress), ex);
                return false;
            }
        }
    }
}