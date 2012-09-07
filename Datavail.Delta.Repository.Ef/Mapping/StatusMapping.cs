using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class StatusMapping : ComplexTypeConfiguration<StatusWrapper>
    {
        public StatusMapping()
        {
           Ignore(e=>e.Enum);
        }
    }
}
