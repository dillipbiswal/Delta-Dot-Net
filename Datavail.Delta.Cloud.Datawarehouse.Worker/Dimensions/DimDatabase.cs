using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimDatabase : Type2Dimension
    {
        [Key]
        public int DatabaseKey { get; set; }
        public Guid DatabaseId { get; set; }
        public int InstanceKey { get; set; }
        public Guid InstanceId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }

        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        public DimDatabase(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private DimDatabase() { }

        private static DimDatabase CreateNewDatabaseDimRow(Guid naturalKey, string name, string status, int instanceKey, Guid instanceId)
        {
            var instanceDimRow = new DimDatabase
            {
                DatabaseId = naturalKey,
                InstanceKey = instanceKey,
                InstanceId = instanceId,
                Name = name,
                Status = status,
                RowStart = new DateTime(2011, 1, 1, 0, 0, 0),
                IsRowCurrent = true
            };

            return instanceDimRow;
        }

        [SqlAzureRetry]
        public static int GetSurrogateKeyFromNaturalKey(Guid naturalKey, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var database = ctx.DimDatabases.FirstOrDefault(t => t.DatabaseId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return database != null ? database.InstanceKey : -1;
            }
        }

        [SqlAzureRetry]
        public void Update()
        {
            try
            {
                var domainDatabases = _repository.GetAll<Database>();

                using (var ctx = new DeltaDwContext())
                {
                    foreach (var domainDatabase in domainDatabases)
                    {
                        var instanceDim = ctx.DimInstances.FirstOrDefault(t => t.InstanceId == domainDatabase.Instance.Id && t.IsRowCurrent);
                        var databaseDim = ctx.DimDatabases.FirstOrDefault(i => i.DatabaseId == domainDatabase.Id && i.IsRowCurrent);

                        if (instanceDim != null)
                        {
                            if (databaseDim == null)
                            {
                                var newInstanceDimRow = CreateNewDatabaseDimRow(domainDatabase.Id, domainDatabase.Name, domainDatabase.Status.Enum.ToString(), instanceDim.InstanceKey, instanceDim.InstanceId);
                                ctx.DimDatabases.Add(newInstanceDimRow);
                                ctx.SaveChanges();
                            }
                            else
                            {
                                if (DataChanged(databaseDim, domainDatabase))
                                {
                                    databaseDim.RowEnd = DateTime.UtcNow;
                                    databaseDim.IsRowCurrent = false;

                                    var newDatabaseDimRow = CreateNewDatabaseDimRow(domainDatabase.Id, domainDatabase.Name,
                                                                                    domainDatabase.Status.Enum.ToString(),
                                                                                    instanceDim.InstanceKey, instanceDim.InstanceId);
                                    ctx.DimDatabases.Add(newDatabaseDimRow);
                                    ctx.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DimDatabase::Update", ex);
            }
        }

        private static bool DataChanged(DimDatabase dwDatabase, Database domainDatabase)
        {
            return dwDatabase.Name != domainDatabase.Name ||
                   dwDatabase.Status != domainDatabase.Status.Enum.ToString();
        }
    }
}