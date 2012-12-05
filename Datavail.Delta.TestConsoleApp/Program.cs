using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;

namespace Datavail.Delta.TestConsoleApp
{
    class Program
    {
        private static void Main(string[] args)
        {
            var repo = new Repository.MongoDb.GenericRepository<DataCollectionMessage>();
            for (var i = 0; i > 2500000; i++)
            {
                var msg = new DataCollectionMessage
                    {
                        Data = "Blah",
                        Hostname = "mytesthost" + i,
                        Timestamp = DateTime.UtcNow,
                        ServerId = Guid.NewGuid(),
                        TenantId = Guid.NewGuid(),
                        IpAddress = "10.1.1.1"
                    };

                repo.Add(msg);
            }

            var test = repo.First(o => o.Hostname == "mytesthost1230000");
            test.InProgress = true;
            repo.Update(test);

            var updated = repo.Get(o => o.InProgress);
        }
    }
}
