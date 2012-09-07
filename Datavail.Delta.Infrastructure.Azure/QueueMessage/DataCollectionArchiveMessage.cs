using System;
using Datavail.Framework.Azure.Queue;

namespace Datavail.Delta.Infrastructure.Azure.QueueMessage
{
    public class DataCollectionArchiveMessage : AzureQueueMessage
    {
        public string Data { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public Guid ServerId { get; set; }
        public Guid TenantId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
