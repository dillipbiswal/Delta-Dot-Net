using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class ClusterMapping : EntityTypeConfiguration<Cluster>
    {
        public ClusterMapping()
        {
            Property(x => x.Name);
            HasKey(x => x.Id);
            HasRequired(x => x.Customer).WithMany(y => y.Clusters);
            HasMany(x => x.Nodes);
            HasMany(x => x.VirtualServers);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            ToTable("Clusters");
        }
    }
}