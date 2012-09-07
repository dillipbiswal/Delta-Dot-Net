using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MetricConfigurationMapping : EntityTypeConfiguration<MetricConfiguration>
    {
        public MetricConfigurationMapping()
        {
            ToTable("MetricConfigurations");

            HasKey(x => x.Id);
            Property(x => x.Name).IsRequired();
            HasMany(x => x.MetricThresholds);
            HasMany(x => x.Schedules).WithRequired(y => y.MetricConfiguration);

            Ignore(x => x.Parent);
            HasOptional(x => x.ParentCustomer);
            HasOptional(x => x.ParentMetric);
            HasOptional(x => x.ParentMetricInstance);
            HasOptional(x => x.ParentServer);
            HasOptional(x => x.ParentServerGroup);
            HasOptional(x => x.ParentTenant);
            
        }
    }
}