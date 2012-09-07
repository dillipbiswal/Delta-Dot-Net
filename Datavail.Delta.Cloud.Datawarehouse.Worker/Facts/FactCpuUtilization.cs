namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Facts
{
    public class FactCpuUtilization : ServerScopedFact
    {
        public double PercentageCpuUsed { get; set; }
    }
}