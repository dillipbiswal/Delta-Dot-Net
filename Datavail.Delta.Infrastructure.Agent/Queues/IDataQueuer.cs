namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    public interface IDataQueuer
    {
        void Queue(string data);
    }
}