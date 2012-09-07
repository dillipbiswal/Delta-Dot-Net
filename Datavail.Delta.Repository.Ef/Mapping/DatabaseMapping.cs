using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class DatabaseMapping : EntityTypeConfiguration<Database>
    {
        public DatabaseMapping()
        {
            Property(x => x.Name);
            HasKey(x => x.Id);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            ToTable("Databases");
        }
    }
}
