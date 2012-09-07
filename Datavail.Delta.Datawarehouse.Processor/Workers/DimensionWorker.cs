using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datavail.Delta.Repository.EfWithMigrations;

namespace Datavail.Delta.Datawarehouse.Processor.Workers
{
    public class DimensionWorker
    {
        public void Run()
        {
            using (var context = new DeltaDbContext())
            {
                foreach (var server in context.Servers)
                {
                    
                }
            }
        }
    }
}
