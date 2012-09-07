using System;
using System.Collections.Generic;
using Datavail.Delta.Domain;
using Datavail.Delta.Repository.Interface;
using System.Linq;

namespace Datavail.Delta.Repository.Mock
{
    public class ServerRepository : RepositoryBase<Server>, IServerRepository
    {
        public IEnumerable<Server> GetByStatus(ServerStatus status)
        {
            return EntityList.Where(e => e.Status == status).AsEnumerable();
        }

        public IEnumerable<Server> GetByCustomer(Guid customerId)
        {
            return EntityList.Where(e => e.Customer.Id == customerId).AsEnumerable();
        }

        public IEnumerable<Server> GetByCustomerByStatus(System.Guid customerId, ServerStatus status)
        {
            return EntityList.Where(e => e.Customer.Id == customerId && e.Status == status).AsEnumerable();
        }

        public void CreateTestData()
        {
            var guids = new Guid[]
                            {
                                new Guid("{92872C0F-B2C0-4019-B342-7F35625C28B8}"),
                                new Guid("{95DB0B81-F36F-4707-A8E5-0E6C50D7434D}"),
                                new Guid("{821F9CE2-6197-4C75-B1AA-70F640DBF2FC}"),
                                new Guid("{FA418A85-46E8-4E38-96AA-9657A8CBCDD2}"),
                                new Guid("{A0786AC3-9DCE-4377-B538-B8CD7F885BCF}"),
                                new Guid("{0B6FC789-FABD-4825-AD6B-5A8B3891C520}"),
                                new Guid("{D7AD33B2-80A7-45D4-995E-8F5B852A06C4}"),
                                new Guid("{BF689715-B2C3-47B8-BCB6-957B6E48E9D7}"),
                                new Guid("{2992E9A4-9189-4E8E-9FBE-9875DB1FC849}"),
                                new Guid("{41DFA73B-F903-476B-BFA4-820C9E121243}"),
                            };

            for (var i = 0; i < 10; i++)
            {
                var server = Server.NewServer(guids[i], "host" + i, "192.168.1." + i);
                if (i % 2 == 0)
                {
                    server.Status = ServerStatus.Active;
                }

                EntityList.Add(server);
            }
        }
    }
}