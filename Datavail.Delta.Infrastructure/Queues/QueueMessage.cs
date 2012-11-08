using System;

namespace Datavail.Delta.Infrastructure.Queues
{
    [Serializable]
    public abstract class QueueMessage
    {
        [NonSerialized]
        protected object OriginalMessage;
        
        public string Id { get; set; }
        public ulong PopReceipt { get; set; }
        public int DequeueCount { get; set; }

        public object GetOriginalMessage()
        {
            return OriginalMessage;
        }

        public void SetOriginalMessage(object message)
        {
            OriginalMessage = message;
        }
    }
}
