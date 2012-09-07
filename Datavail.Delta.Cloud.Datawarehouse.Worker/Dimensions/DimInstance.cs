using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimInstance : Type2Dimension
    {
        [Key]
        public int InstanceKey { get; set; }
        public Guid InstanceId { get; set; }
        public int ServerKey { get; set; }
        public Guid ServerId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string DatabaseVersion { get; set; }

        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        public DimInstance(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private DimInstance() { }

        private static DimInstance CreateNewInstanceDimRow(Guid naturalKey, string name, string status, string databaseVersion, int serverKey, Guid serverId)
        {
            var serverDimRow = new DimInstance
            {
                InstanceId = naturalKey,
                ServerKey = serverKey,
                ServerId = serverId,
                Name = name,
                Status = status,
                DatabaseVersion = databaseVersion,
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
                var instance = ctx.DimInstances.FirstOrDefault(t => t.InstanceId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return instance != null ? instance.InstanceKey : -1;
            }
        }

        [SqlAzureRetry]
        public void Update()
        {
            try
            {
                var databaseInstances = _repository.GetAll<DatabaseInstance>();

                using (var ctx = new DeltaDwContext())
                {
                    foreach (var databaseInstance in databaseInstances)
                    {
                        var serverDim = ctx.DimServers.FirstOrDefault(t => t.ServerId == databaseInstance.Server.Id && t.IsRowCurrent);
                        var instance = ctx.DimInstances.FirstOrDefault(i => i.InstanceId == databaseInstance.Id && i.IsRowCurrent);

                        if (serverDim != null)
                        {
                            if (instance == null)
                            {
                                var newInstanceDimRow = CreateNewInstanceDimRow(databaseInstance.Id, databaseInstance.Name, databaseInstance.Status.Enum.ToString(), databaseInstance.DatabaseVersion.Enum.ToString(), serverDim.ServerKey, serverDim.ServerId);
                                ctx.DimInstances.Add(newInstanceDimRow);
                                ctx.SaveChanges();
                            }
                            else
                            {
                                if (DataChanged(instance, databaseInstance))
                                {
                                    instance.RowEnd = DateTime.UtcNow;
                                    instance.IsRowCurrent = false;

                                    var newInstanceDimRow = CreateNewInstanceDimRow(databaseInstance.Id, databaseInstance.Name,
                                                                                    databaseInstance.Status.Enum.ToString(),
                                                                                    databaseInstance.DatabaseVersion.Enum.ToString(), serverDim.ServerKey, serverDim.ServerId);
                                    ctx.DimInstances.Add(newInstanceDimRow);
                                    ctx.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DimInstance::Update", ex);
            }
        }

        private static bool DataChanged(DimInstance dwInstance, DatabaseInstance domainInstance)
        {
            return dwInstance.Name != domainInstance.Name ||
                   dwInstance.Status != domainInstance.Status.Enum.ToString() ||
                   dwInstance.DatabaseVersion != domainInstance.DatabaseVersion.Enum.ToString();
        }
    }
}