namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Facts
{
    public class FactSqlAgentJobStatus : InstanceScopedFact
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string RunDuration { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
    }
}