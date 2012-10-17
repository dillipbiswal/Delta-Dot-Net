using System;
using System.Collections.Generic;

namespace Datavail.Delta.Infrastructure.Queues
{
    public delegate void OnReceiveMessage(QueueMessage message);

    public interface IQueue<TMessage> : IDisposable where TMessage : QueueMessage
    {
        void AddMessage(TMessage message);
        void Clear();
        void Delete();
        void DeleteMessage(TMessage message);
        int GetApproximateMessageCount();
        TMessage GetMessage();
        IEnumerable<TMessage> GetMessages(int messageCount);
    }
}