using System;

namespace Datavail.Delta.Infrastructure.Queues.Messages
{
    public class DataCollectionMessageWithError : QueueMessage
    {
        public string Data { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public Guid ServerId { get; set; }
        public Guid TenantId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ExceptionMessage { get; set; }
    }
}
