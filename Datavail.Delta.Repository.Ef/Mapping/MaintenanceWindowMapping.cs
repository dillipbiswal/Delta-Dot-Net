using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MaintenanceWindowMapping : EntityTypeConfiguration<MaintenanceWindow>
    {
        public MaintenanceWindowMapping()
        {
            ToTable("MaintenanceWindows");

            HasKey(x => x.Id);

            Ignore(x => x.Parent);
            HasOptional(x => x.Customer);
            HasOptional(x => x.Metric);
            HasOptional(x => x.MetricInstance);
            HasOptional(x => x.Server);
            HasOptional(x => x.ServerGroup);
            HasOptional(x => x.Tenant);
        }
    }
}