namespace Datavail.Delta.Infrastructure.Queues
{
    public static class QueueNames
    {
        public static string CheckInQueue { get { return "CheckIns"; } }
        public static string DataWarehouseQueue { get { return "DataWarehouse"; } }
        public static string ErrorQueue { get { return "Errors"; } }
        public static string IncidentProcessorQueue { get { return "IncidentProcessor"; } }
        public static string OpenIncidentQueue { get { return "OpenIncident"; } }
        public static string TestQueue { get { return "Test"; } }
        public static string InventoryQueue { get { return "Inventory"; } }
        public static string AgentErrorQueue { get { return "AgentError"; } }
    }
}