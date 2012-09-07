using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class RoleMapping : EntityTypeConfiguration<Role>
    {
        public RoleMapping()
        {
            ToTable("Roles");
            HasKey(x => x.Id);
            Property(x => x.Name).IsRequired().HasMaxLength(1024);
        }
    }
}
