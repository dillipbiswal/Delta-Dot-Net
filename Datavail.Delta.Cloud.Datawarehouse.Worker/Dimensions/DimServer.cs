using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimServer : Type2Dimension
    {
        [Key]
        public int ServerKey { get; set; }
        public Guid ServerId { get; set; }
        public int CustomerKey { get; set; }
        public Guid? CustomerId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string Status { get; set; }
        public string AgentVersion { get; set; }
        public bool IsVirtual { get; set; }

        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        public DimServer(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private DimServer() { }

        private static DimServer CreateNewServerDimRow(Guid naturalKey, string hostname, string status, string ipAddress, string agentVersion, int customerKey, Guid? customerId, bool isVirtual)
        {
            var serverDimRow = new DimServer
            {
                ServerId = naturalKey,
                Hostname = hostname,
                CustomerKey = customerKey,
                CustomerId = customerId,
                Status = status,
                IpAddress = ipAddress,
                AgentVersion = agentVersion,
                IsVirtual = isVirtual,
                RowStart = new DateTime(2011, 1, 1, 0, 0, 0),
                IsRowCurrent = true
            };

            return serverDimRow;
        }

        [SqlAzureRetry]
        public static int GetSurrogateKeyFromNaturalKey(Guid naturalKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var server = ctx.DimServers.FirstOrDefault(t => t.ServerId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return server != null ? server.ServerKey : -1;
            }
        }

        [SqlAzureRetry]
        public static int GetCustomerSurrogateKeyFromNaturalKey(Guid naturalKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var server = ctx.DimServers.FirstOrDefault(t => t.ServerId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return server != null ? server.ServerKey : -1;
            }
        }

        [SqlAzureRetry]
        public void Update()
        {
            try
            {
                var servers = _repository.GetAll<Server>();

                using (var ctx = new DeltaDwContext())
                {
                    foreach (var server in servers)
                    {
                        var customerKey = server.Customer != null ? DimCustomer.GetSurrogateKeyFromNaturalKey(server.Customer.Id, DateTime.UtcNow) : -1;
                        var customerId = server.Customer != null ? server.Customer.Id : default(Guid);
                        var serverDim = ctx.DimServers.FirstOrDefault(t => t.ServerId == server.Id && t.IsRowCurrent);

                        if (serverDim == null)
                        {
                            var newServerDimRow = CreateNewServerDimRow(server.Id, server.Hostname, server.Status.Enum.ToString(), server.IpAddress, server.AgentVersion, customerKey, customerId, server.IsVirtual);
                            ctx.DimServers.Add(newServerDimRow);
                            ctx.SaveChanges();
                        }
                        else
                        {
                            if (DataChanged(serverDim, server, customerKey))
                            {
                                serverDim.RowEnd = DateTime.UtcNow;
                                serverDim.IsRowCurrent = false;

                                var newServerDimRow = CreateNewServerDimRow(server.Id, server.Hostname,
                                                                            server.Status.Enum.ToString(),
                                                                            server.IpAddress, server.AgentVersion,
                                                                            customerKey, customerId, server.IsVirtual);

                                ctx.DimServers.Add(newServerDimRow);
                                ctx.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DimServer::Update", ex);
            }
        }

        private static bool DataChanged(DimServer dwServer, Server domainServer, int customerKey)
        {
            return dwServer.Hostname != domainServer.Hostname ||
                   dwServer.Status != domainServer.Status.Enum.ToString() ||
                   dwServer.IpAddress != domainServer.IpAddress ||
                   dwServer.AgentVersion != domainServer.AgentVersion ||
                   dwServer.CustomerKey != customerKey;
        }
    }
}