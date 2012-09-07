using System;
using System.Linq;
using System.Xml.Linq;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Facts
{
    public class FactRunner
    {
        private readonly IQueue<DataCollectionArchiveMessage> _dataCollectionMessage;
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        public FactRunner(IQueue<DataCollectionArchiveMessage> dataCollectionMessage, IDeltaLogger logger, IRepository repository)
        {
            _dataCollectionMessage = dataCollectionMessage;
            _logger = logger;
            _repository = repository;
        }

        [SqlAzureRetry]
        public void CreateCpuFactRow(Guid tenantId, Guid serverId, DateTime timestamp, string data)
        {
            using (var ctx = new DeltaDwContext())
            {
                var dateKey = DimDate.GetSurrogateKeyFromTimestamp(timestamp);
                var timeKey = DimTime.GetSurrogateKeyFromTimestamp(timestamp);
                var tenantKey = DimTenant.GetSurrogateKeyFromNaturalKey(tenantId, timestamp);
                var serverKey = DimServer.GetSurrogateKeyFromNaturalKey(serverId, timestamp);
                var customerKey = DimCustomer.GetSurrogateKeyFromServerSurrogateKey(serverKey, timestamp);
                var customerId = DimCustomer.GetNaturalKeyFromServerSurrogateKey(serverKey, timestamp);

                var xmlData = XElement.Parse(data);

                var percentageCpuUsed = xmlData.Attribute("percentageCpuUsed").Value;


                var fact = new FactCpuUtilization
                               {
                                   DateKey = dateKey,
                                   TimeKey = timeKey,
                                   CustomerKey = customerKey,
                                   CustomerId = customerId,
                                   TenantKey = tenantKey,
                                   TenantId = tenantId,
                                   ServerKey = serverKey,
                                   ServerId = serverId,
                                   PercentageCpuUsed = double.Parse(percentageCpuUsed)
                               };

                ctx.FactCpuUtilizations.Add(fact);
                ctx.SaveChanges();
            }
        }

        [SqlAzureRetry]
        public void CreateRamFactRow(Guid tenantId, Guid serverId, DateTime timestamp, string data)
        {
            using (var ctx = new DeltaDwContext())
            {
                var dateKey = DimDate.GetSurrogateKeyFromTimestamp(timestamp);
                var timeKey = DimTime.GetSurrogateKeyFromTimestamp(timestamp);
                var tenantKey = DimTenant.GetSurrogateKeyFromNaturalKey(tenantId, timestamp);
                var serverKey = DimServer.GetSurrogateKeyFromNaturalKey(serverId, timestamp);
                var customerKey = DimCustomer.GetSurrogateKeyFromServerSurrogateKey(serverKey, timestamp);
                var customerId = DimCustomer.GetNaturalKeyFromServerSurrogateKey(serverKey, timestamp);

                var xmlData = XElement.Parse(data);

                var totalPhysicalMemoryBytes = xmlData.Attribute("totalPhysicalMemoryBytes").Value;
                var totalPhysicalMemoryFriendly = xmlData.Attribute("totalPhysicalMemoryFriendly").Value;
                var totalVirtualMemoryBytes = xmlData.Attribute("totalVirtualMemoryBytes").Value;
                var totalVirtualMemoryFriendly = xmlData.Attribute("totalVirtualMemoryFriendly").Value;
                var availablePhysicalMemoryBytes = xmlData.Attribute("availablePhysicalMemoryBytes").Value;
                var availablePhysicalMemoryFriendly = xmlData.Attribute("availablePhysicalMemoryFriendly").Value;
                var availableVirtualMemoryBytes = xmlData.Attribute("availableVirtualMemoryBytes").Value;
                var availableVirtualMemoryFriendly = xmlData.Attribute("availableVirtualMemoryFriendly").Value;
                var percentagePhysicalMemoryAvailable = xmlData.Attribute("percentagePhysicalMemoryAvailable").Value;
                var percentageVirtualMemoryAvailable = xmlData.Attribute("percentageVirtualMemoryAvailable").Value;

                var fact = new FactRam
                               {
                                   DateKey = dateKey,
                                   TimeKey = timeKey,
                                   CustomerKey = customerKey,
                                   CustomerId = customerId,
                                   TenantKey = tenantKey,
                                   TenantId = tenantId,
                                   ServerKey = serverKey,
                                   ServerId = serverId,
                                   TotalPhysicalMemoryBytes = long.Parse(totalPhysicalMemoryBytes),
                                   TotalPhysicalMemoryFriendly = totalPhysicalMemoryFriendly,
                                   TotalVirtualMemoryBytes = long.Parse(totalVirtualMemoryBytes),
                                   TotalVirtualMemoryFriendly = totalVirtualMemoryFriendly,
                                   AvailablePhysicalMemoryBytes = long.Parse(availablePhysicalMemoryBytes),
                                   AvailablePhysicalMemoryFriendly = availablePhysicalMemoryFriendly,
                                   AvailableVirtualMemoryBytes = long.Parse(availableVirtualMemoryBytes),
                                   AvailableVirtualMemoryFriendly = availableVirtualMemoryFriendly,
                                   PercentagePhysicalMemoryAvailable = double.Parse(percentagePhysicalMemoryAvailable),
                                   PercentageVirtualMemoryAvailable = double.Parse(percentageVirtualMemoryAvailable)
                               };

                ctx.FactRam.Add(fact);
                ctx.SaveChanges();
            }
        }

        [SqlAzureRetry]
        public void CreateSqlAgentJobFactRow(Guid tenantId, Guid serverId, DateTime timestamp, string data)
        {
            using (var ctx = new DeltaDwContext())
            {
                var dateKey = DimDate.GetSurrogateKeyFromTimestamp(timestamp);
                var timeKey = DimTime.GetSurrogateKeyFromTimestamp(timestamp);
                var tenantKey = DimTenant.GetSurrogateKeyFromNaturalKey(tenantId, timestamp);
                var serverKey = DimServer.GetSurrogateKeyFromNaturalKey(serverId, timestamp);
                var customerKey = DimCustomer.GetSurrogateKeyFromServerSurrogateKey(serverKey, timestamp);
                var customerId = DimCustomer.GetNaturalKeyFromServerSurrogateKey(serverKey, timestamp);

                var xmlData = XElement.Parse(data);
                if (xmlData != null)
                    foreach (var dataCollection in xmlData.Elements("JobStatus"))
                    {
                        var jobId = dataCollection.Attribute("jobId").Value;
                        var jobStatus = dataCollection.Attribute("jobStatus").Value;
                        var message = dataCollection.Attribute("message").Value;
                        var stepId = dataCollection.Attribute("stepId").Value;
                        var stepName = dataCollection.Attribute("stepName").Value;
                        var runDuration = dataCollection.Attribute("runDuration").Value;
                        var instanceName = dataCollection.Attribute("instanceName").Value;
                        var server = _repository.GetByKey<Server>(serverId);
                        var instanceId = server.Instances.FirstOrDefault(i => i.Name == instanceName);
                        if (instanceId == null && server.Cluster != null)
                        {
                            foreach (var virtualServer in server.Cluster.VirtualServers)
                            {
                                if (virtualServer.Instances.FirstOrDefault(i => i.Name == instanceName)!=null)
                                {
                                    instanceId = virtualServer.Instances.FirstOrDefault(i => i.Name == instanceName);
                                }
                            }
                        }
                        var instanceKey = DimInstance.GetSurrogateKeyFromNaturalKey(instanceId.Id, timestamp);

                        var fact = new FactSqlAgentJobStatus
                                       {
                                           DateKey = dateKey,
                                           TimeKey = timeKey,
                                           CustomerKey = customerKey,
                                           CustomerId = customerId,
                                           TenantKey = tenantKey,
                                           TenantId = tenantId,
                                           ServerKey = serverKey,
                                           ServerId = serverId,
                                           InstanceKey = instanceKey,
                                           InstanceId = instanceId.Id,
                                           JobId = jobId,
                                           Status = jobStatus,
                                           Message = message,
                                           RunDuration = runDuration,
                                           StepId = Int32.Parse(stepId),
                                           StepName = stepName
                                       };

                        ctx.FactSqlAgentJobStatuses.Add(fact);
                        ctx.SaveChanges();
                    }
            }
        }

        [SqlAzureRetry]
        public void CreateDatabaseStatusFactRow(Guid tenantId, Guid serverId, DateTime timestamp, string data)
        {
            using (var ctx = new DeltaDwContext())
            {
                var dateKey = DimDate.GetSurrogateKeyFromTimestamp(timestamp);
                var timeKey = DimTime.GetSurrogateKeyFromTimestamp(timestamp);
                var tenantKey = DimTenant.GetSurrogateKeyFromNaturalKey(tenantId, timestamp);
                var serverKey = DimServer.GetSurrogateKeyFromNaturalKey(serverId, timestamp);
                var customerKey = DimCustomer.GetSurrogateKeyFromServerSurrogateKey(serverKey, timestamp);
                var customerId = DimCustomer.GetNaturalKeyFromServerSurrogateKey(serverKey, timestamp);

                var xmlData = XElement.Parse(data);
                if (xmlData != null)
                {
                    var status = xmlData.Attribute("status").Value;
                    var instanceName = xmlData.Attribute("instanceName").Value;
                    var server = _repository.GetByKey<Server>(serverId);
                    var instanceId = server.Instances.FirstOrDefault(i => i.Name == instanceName);
                    if (instanceId == null && server.Cluster != null)
                    {
                        foreach (var virtualServer in server.Cluster.VirtualServers)
                        {
                            if (virtualServer.Instances.FirstOrDefault(i => i.Name == instanceName) != null)
                            {
                                instanceId = virtualServer.Instances.FirstOrDefault(i => i.Name == instanceName);
                            }
                        }
                    }
                    var instanceKey = DimInstance.GetSurrogateKeyFromNaturalKey(instanceId.Id, timestamp);

                    var fact = new FactDatabaseStatus
                                   {
                                       DateKey = dateKey,
                                       TimeKey = timeKey,
                                       CustomerKey = customerKey,
                                       CustomerId = customerId,
                                       TenantKey = tenantKey,
                                       TenantId = tenantId,
                                       ServerKey = serverKey,
                                       ServerId = serverId,
                                       InstanceKey = instanceKey,
                                       InstanceId = instanceId.Id,
                                       Status = status,
                                   };

                    ctx.FactDatabaseStatuses.Add(fact);
                    ctx.SaveChanges();
                }
            }
        }

        public void Update()
        {
            try
            {
                var message = _dataCollectionMessage.GetMessage();

                while (message != null)
                {
                    try
                    {
                        if (message.Data.Contains("SqlAgentJobInventoryPluginOutput"))
                        {
                            _dataCollectionMessage.DeleteMessage(message);
                        }
                        
                        if (message.Data.Contains("DatabaseInventoryPluginOutput"))
                        {
                            _dataCollectionMessage.DeleteMessage(message);
                        }

                        if (message.Data.Contains("CpuPluginOutput"))
                        {
                            CreateCpuFactRow(message.TenantId, message.ServerId, message.Timestamp, message.Data);
                            _dataCollectionMessage.DeleteMessage(message);
                        }

                        if (message.Data.Contains("RamPluginOutput"))
                        {
                            CreateRamFactRow(message.TenantId, message.ServerId, message.Timestamp, message.Data);
                            _dataCollectionMessage.DeleteMessage(message);
                        }
                        
                        if (message.Data.Contains("DatabaseJobStatusPluginOutput"))
                        {
                            CreateSqlAgentJobFactRow(message.TenantId, message.ServerId, message.Timestamp, message.Data);
                            _dataCollectionMessage.DeleteMessage(message);
                        }

                        if (message.Data.Contains("DatabaseStatusPluginOutput"))
                        {
                            CreateDatabaseStatusFactRow(message.TenantId, message.ServerId, message.Timestamp, message.Data);
                            _dataCollectionMessage.DeleteMessage(message);
                        }

                        message = _dataCollectionMessage.GetMessage();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Error in FactRunner::Update", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error reading messages from Queue in FactRunner::Update", ex);
            }
        }
    }
}