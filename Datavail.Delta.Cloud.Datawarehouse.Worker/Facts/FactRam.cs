namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Facts
{
    public class FactRam : ServerScopedFact
    {
        public double TotalPhysicalMemoryBytes { get; set; }
        public string TotalPhysicalMemoryFriendly { get; set; }
        public double TotalVirtualMemoryBytes { get; set; }
        public string TotalVirtualMemoryFriendly { get; set; }

        public double AvailablePhysicalMemoryBytes { get; set; }
        public string AvailablePhysicalMemoryFriendly { get; set; }
        public double AvailableVirtualMemoryBytes { get; set; }
        public string AvailableVirtualMemoryFriendly { get; set; }

        public double PercentagePhysicalMemoryAvailable { get; set; }
        public double PercentageVirtualMemoryAvailable { get; set; }
    }
}