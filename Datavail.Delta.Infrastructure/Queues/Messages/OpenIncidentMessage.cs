
using System;

namespace Datavail.Delta.Infrastructure.Queues.Messages
{
    public class OpenIncidentMessage : QueueMessage
    {
        public string AdditionalData { get; set; }
        public string Body { get; set; }
        public Guid MetricInstanceId { get; set; }
        public int Priority { get; set; }
        public string Summary { get; set; }
    }
}
