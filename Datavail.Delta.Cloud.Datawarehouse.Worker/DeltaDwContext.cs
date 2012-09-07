using System.Data.Entity;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Facts;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker
{
    public class DeltaDwContext : DbContext
    {
        public DbSet<DimDate> DimDates { get; set; }
        public DbSet<DimTime> DimTimes { get; set; }
        public DbSet<DimTenant> DimTenants { get; set; }
        public DbSet<DimCustomer> DimCustomers { get; set; }
        public DbSet<DimServer> DimServers { get; set; }
        public DbSet<DimInstance> DimInstances { get; set; }
        public DbSet<DimDatabase> DimDatabases { get; set; }
        public DbSet<DimSqlAgentJob> DimSqlAgentJobs { get; set; }

        public DbSet<FactServerCheckIn> FactServerCheckIns { get; set; }
        public DbSet<FactCpuUtilization> FactCpuUtilizations { get; set; }
        public DbSet<FactRam> FactRam { get; set; }
        public DbSet<FactSqlAgentJobStatus> FactSqlAgentJobStatuses { get; set; }
        public DbSet<FactDatabaseStatus> FactDatabaseStatuses { get; set; }
    }
}
