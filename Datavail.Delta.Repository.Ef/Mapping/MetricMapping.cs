using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MetricMapping : EntityTypeConfiguration<Metric>
    {
        public MetricMapping()
        {
            ToTable("Metrics");

            HasKey(x => x.Id);
            Property(x => x.AdapterAssembly).IsRequired();
            Property(x => x.AdapterClass).IsRequired();
            Property(x => x.AdapterVersion).IsRequired();
            Property(x => x.MetricType.Value).HasColumnName("MetricType_Id");
            Property(x => x.MetricThresholdType.Value).HasColumnName("MetricThresholdType_Id");
            Property(x => x.Name).IsRequired();
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            Property(x => x.DatabaseVersion.Value).HasColumnName("DatabaseVersion_Id");
            HasMany(x => x.MetricConfigurations);
        }
    }
}