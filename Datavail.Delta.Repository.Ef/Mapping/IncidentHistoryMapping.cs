using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class IncidentHistoryMapping : EntityTypeConfiguration<IncidentHistory>
    {
        public IncidentHistoryMapping()
        {
            HasKey(x => x.Id);
            HasRequired(x => x.MetricInstance);
            ToTable("IncidentHistories");
        }
    }
}