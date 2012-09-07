using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class ServerMapping : EntityTypeConfiguration<Server>
    {
        public ServerMapping()
        {
            HasKey(x => x.Id);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            HasRequired(x => x.Tenant).WithMany(x => x.Servers).WillCascadeOnDelete(true);
            HasMany(x => x.MetricInstances);
            HasOptional(x => x.Cluster).WithMany(x => x.Nodes);
            HasOptional(x => x.VirtualServerParent).WithMany(x => x.VirtualServers);
            ToTable("Servers");
        }
    }
}