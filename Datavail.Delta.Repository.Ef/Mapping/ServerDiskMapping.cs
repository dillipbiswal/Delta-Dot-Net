using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class ServerDiskMapping : EntityTypeConfiguration<ServerDisk>
    {
        public ServerDiskMapping()
        {
            HasKey(x => x.Id);
            ToTable("ServerDisks");
        }
    }
}