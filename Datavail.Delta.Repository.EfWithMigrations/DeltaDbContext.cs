using System.Data.Entity;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.EfWithMigrations
{
    public class DeltaDbContext : DbContext
    {
        public IDbSet<Cluster> Clusters { get; set; }
        public IDbSet<Customer> Customers { get; set; }
        public IDbSet<Domain.Database> Databases { get; set; }
        public IDbSet<DatabaseInstance> DatabaseInstances { get; set; }
        public IDbSet<IncidentHistory> IncidentHistories { get; set; }
        public IDbSet<MaintenanceWindow> MaintenanceWindows { get; set; }
        public IDbSet<Metric> Metrics { get; set; }
        public IDbSet<MetricConfiguration> MetricConfigurations { get; set; }
        public IDbSet<MetricInstance> MetricInstances { get; set; }
        public IDbSet<MetricThreshold> MetricThresholds { get; set; }
        public IDbSet<MetricThresholdHistory> MetricThresholdHistories { get; set; }
        public IDbSet<Role> Roles { get; set; }
        public IDbSet<Schedule> Schedules { get; set; }
        public IDbSet<Server> Servers { get; set; }
        public IDbSet<ServerDisk> ServerDisks { get; set; }
        public IDbSet<ServerGroup> ServerGroups { get; set; }
        public IDbSet<SqlAgentJob> SqlAgentJobs { get; set; }
        public IDbSet<Tenant> Tenants { get; set; }
        public IDbSet<User> Users { get; set; }

        public DeltaDbContext()
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Clusters
            modelBuilder.Entity<Cluster>().Property(p => p.Status).HasColumnName("Status_Id");
            modelBuilder.Entity<Cluster>().HasRequired(p => p.Customer).WithMany(p => p.Clusters);

            //Customers
            modelBuilder.Entity<Customer>().Property(p => p.Status).HasColumnName("Status_Id");
            modelBuilder.Entity<Customer>().HasRequired(p => p.Tenant).WithMany(p => p.Customers).WillCascadeOnDelete();

            //Databases
            modelBuilder.Entity<Domain.Database>().Property(p => p.Status).HasColumnName("Status_Id");

            //Databases Instances
            modelBuilder.Entity<DatabaseInstance>().Property(p => p.Status).HasColumnName("Status_Id");
            modelBuilder.Entity<DatabaseInstance>().Property(p => p.DatabaseVersion).HasColumnName("DatabaseVersion_Id");

            //Incident Histories
            modelBuilder.Entity<IncidentHistory>().HasRequired(p => p.MetricInstance);

            //Metric
            modelBuilder.Entity<Metric>().Property(p => p.MetricType).HasColumnName("MetricType_Id");
            modelBuilder.Entity<Metric>().Property(p => p.MetricThresholdType).HasColumnName("MetricThresholdType_Id");
            modelBuilder.Entity<Metric>().Property(p => p.Status).HasColumnName("Status_Id");
            modelBuilder.Entity<Metric>().Property(p => p.DatabaseVersion).HasColumnName("DatabaseVersion_Id");

            //MetricInstance
            modelBuilder.Entity<MetricInstance>().Property(p => p.Status).HasColumnName("Status_Id");

            modelBuilder.Entity<MetricThreshold>().Property(p => p.Severity).HasColumnName("Severity_Value");
            modelBuilder.Entity<MetricThreshold>().Property(p => p.ThresholdComparisonFunction).HasColumnName("ThresholdComparisonFunction_Value");
            modelBuilder.Entity<MetricThreshold>().Property(p => p.ThresholdValueType).HasColumnName("ThresholdValueType_Value");

            //Metric Threshold Histories
            modelBuilder.Entity<MetricThresholdHistory>().HasRequired(p => p.MetricInstance);

            //Schedule
            modelBuilder.Entity<Schedule>().Property(p => p.DayOfWeek).IsOptional().HasColumnName("DayOfWeek");
            modelBuilder.Entity<Schedule>().HasRequired(p => p.MetricConfiguration).WithMany(p => p.Schedules).WillCascadeOnDelete(true);
            modelBuilder.Entity<Schedule>().Property(p => p.ScheduleType).HasColumnName("ScheduleType_Id");

            //Server
            modelBuilder.Entity<Server>().Property(p => p.Status).HasColumnName("Status_Id");
            modelBuilder.Entity<Server>().HasRequired(p => p.Tenant).WithMany(p => p.Servers).WillCascadeOnDelete(true);
            modelBuilder.Entity<Server>().HasOptional(p => p.Cluster).WithMany(p => p.Nodes);
            modelBuilder.Entity<Server>().HasOptional(p => p.VirtualServerParent).WithMany(p => p.VirtualServers);

            //Server Group
            modelBuilder.Entity<ServerGroup>().Property(p => p.Status).HasColumnName("Status_Id");

            //Sql Agent Jobs
            modelBuilder.Entity<SqlAgentJob>().Property(p => p.Status).HasColumnName("Status_Id");

            //Tenant
            modelBuilder.Entity<Tenant>().Property(p => p.Status).HasColumnName("Status_Id");

            modelBuilder.Entity<MaintenanceWindow>().Property(p => p.ParentPreviousStatus).HasColumnName("ParentPreviousStatus_Value");
        }
    }
}