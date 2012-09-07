using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class DatabaseInstanceMapping : EntityTypeConfiguration<DatabaseInstance>
    {
        public DatabaseInstanceMapping()
        {
            HasKey(x => x.Id);
            HasMany(x => x.Databases).WithRequired(y => y.Instance);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            Property(x => x.DatabaseVersion.Value).HasColumnName("DatabaseVersion_Id");
            ToTable("DatabaseInstances");
        }
    }
}