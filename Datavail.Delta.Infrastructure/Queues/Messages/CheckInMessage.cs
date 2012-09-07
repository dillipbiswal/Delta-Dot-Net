using System;

namespace Datavail.Delta.Infrastructure.Queues.Messages
{
    public class CheckInMessage : QueueMessage
    {
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public Guid ServerId { get; set; }
        public Guid TenantId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
