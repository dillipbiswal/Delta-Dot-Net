
namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    public enum OperationType : byte
    {
        Enqueue = 1,
        Dequeue = 2,
        Reinstate = 3
    }
}
