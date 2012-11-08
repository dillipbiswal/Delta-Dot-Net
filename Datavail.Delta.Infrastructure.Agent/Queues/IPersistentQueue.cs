
namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    using System;

    public interface IPersistentQueue : IDisposable
    {
        PersistentQueueSession OpenSession();
    }
}