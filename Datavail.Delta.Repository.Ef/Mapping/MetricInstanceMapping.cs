using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MetricInstanceMapping : EntityTypeConfiguration<MetricInstance>
    {
        public MetricInstanceMapping()
        {
            HasKey(x => x.Id);
            HasRequired(x => x.Server).WithMany(x => x.MetricInstances).WillCascadeOnDelete(true);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            HasRequired(x => x.Metric);
            HasOptional(x => x.DatabaseInstance);
            HasOptional(x => x.Database);
            ToTable("MetricInstances");
        }
    }
}