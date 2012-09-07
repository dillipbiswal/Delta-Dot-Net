using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class MetricTypeMapping : ComplexTypeConfiguration<MetricTypeWrapper>
    {
        public MetricTypeMapping()
        {
           Ignore(e=>e.Enum);
        }
    }
}
