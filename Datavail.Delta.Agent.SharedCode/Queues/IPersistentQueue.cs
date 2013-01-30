
using System;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public interface IPersistentQueue : IDisposable
    {
        PersistentQueueSession OpenSession();
    }
}