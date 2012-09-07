using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MetricThresholdMapping : EntityTypeConfiguration<MetricThreshold>
    {
        public MetricThresholdMapping()
        {
            ToTable("MetricThresholds");

            HasKey(x => x.Id);
        }
    }
}