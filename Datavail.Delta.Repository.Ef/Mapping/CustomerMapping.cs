using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class CustomerMapping : EntityTypeConfiguration<Customer>
    {
        public CustomerMapping()
        {
            HasKey(x => x.Id);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            HasRequired(x=>x.Tenant).WithMany(x=>x.Customers).WillCascadeOnDelete(true);
            ToTable("Customers");
        }
    }
}