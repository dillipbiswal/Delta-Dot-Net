using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class ServerGroupMapping : EntityTypeConfiguration<ServerGroup>
    {
        public ServerGroupMapping()
        {
            ToTable("ServerGroups");

            HasKey(x => x.Id);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            Property(x => x.Name).IsRequired().HasMaxLength(1024);
            HasMany(x => x.MetricConfigurations);
            Property(x => x.Priority);
            HasMany(x => x.Servers);
        }
    }
}