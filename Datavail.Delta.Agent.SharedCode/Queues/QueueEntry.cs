using System;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    internal class QueueEntry
    {
        internal QueueEntry()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public DateTime InsertTime { get; set; }
        public string Data { get; set; }
    }
}
