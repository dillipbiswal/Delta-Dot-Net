
namespace Datavail.Delta.IncidentProcessor
{
    public abstract class WorkerBase
    {
        public bool ServiceStarted { get; set; }
        public abstract void Run();
    }
}
