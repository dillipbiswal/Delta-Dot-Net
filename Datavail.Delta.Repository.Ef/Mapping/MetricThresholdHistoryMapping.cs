using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MetricThresholdHistoryMapping : EntityTypeConfiguration<MetricThresholdHistory>
    {
        public MetricThresholdHistoryMapping()
        {
            HasKey(x => x.Id);
            HasRequired(x => x.MetricThreshold);
            ToTable("MetricThresholdHistories");
        }
    }
}