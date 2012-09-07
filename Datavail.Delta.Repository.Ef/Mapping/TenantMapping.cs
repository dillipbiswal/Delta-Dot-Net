using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class TenantMapping : EntityTypeConfiguration<Tenant>
    {
        public TenantMapping()
        {
            HasKey(x => x.Id);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            ToTable("Tenants");
        }
    }
}