using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using Datavail.Delta.Datawarehouse.Processor.Dimensions;

namespace Datavail.Delta.Datawarehouse.Processor
{
    public class DeltaDwContext : DbContext
    {
        public DbSet<Server> Servers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Server>().ToTable("DimServers");
        }
    }
}
