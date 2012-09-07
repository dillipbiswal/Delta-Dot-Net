using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimSqlAgentJob : Type2Dimension
    {
        [Key]
        public int SqlAgentJobKey { get; set; }
        public Guid SqlAgentJobId { get; set; }
        public int InstanceKey { get; set; }
        public Guid InstanceId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }

        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        public DimSqlAgentJob(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private DimSqlAgentJob() { }

        private static DimSqlAgentJob CreateNewSqlAgentJobDimRow(Guid naturalKey, string name, string status, int instanceKey, Guid instanceId)
        {
            var instanceDimRow = new DimSqlAgentJob
            {
                SqlAgentJobId = naturalKey,
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
                var sqlAgentJob = ctx.DimSqlAgentJobs.FirstOrDefault(t => t.SqlAgentJobId == naturalKey && (t.RowStart <= timestamp && t.IsRowCurrent || t.RowStart <= timestamp && t.RowEnd >= timestamp));
                return sqlAgentJob != null ? sqlAgentJob.InstanceKey : -1;
            }
        }

        [SqlAzureRetry]
        public void Update()
        {
            try
            {
                var domainSqlAgentJobs = _repository.GetAll<SqlAgentJob>();

                using (var ctx = new DeltaDwContext())
                {
                    foreach (var domainSqlAgentJob in domainSqlAgentJobs)
                    {
                        var instanceDim = ctx.DimInstances.FirstOrDefault(t => t.InstanceId == domainSqlAgentJob.Instance.Id && t.IsRowCurrent);
                        var sqlAgentJobDim = ctx.DimSqlAgentJobs.FirstOrDefault(i => i.SqlAgentJobId == domainSqlAgentJob.Id && i.IsRowCurrent);

                        if (instanceDim != null)
                        {
                            if (sqlAgentJobDim == null)
                            {
                                var newInstanceDimRow = CreateNewSqlAgentJobDimRow(domainSqlAgentJob.Id, domainSqlAgentJob.Name, domainSqlAgentJob.Status.Enum.ToString(), instanceDim.InstanceKey, instanceDim.InstanceId);
                                ctx.DimSqlAgentJobs.Add(newInstanceDimRow);
                                ctx.SaveChanges();
                            }
                            else
                            {
                                if (DataChanged(sqlAgentJobDim, domainSqlAgentJob))
                                {
                                    sqlAgentJobDim.RowEnd = DateTime.UtcNow;
                                    sqlAgentJobDim.IsRowCurrent = false;

                                    var newSqlAgentJobDimRow = CreateNewSqlAgentJobDimRow(domainSqlAgentJob.Id, domainSqlAgentJob.Name,
                                                                                    domainSqlAgentJob.Status.Enum.ToString(),
                                                                                    instanceDim.InstanceKey, instanceDim.InstanceId);
                                    ctx.DimSqlAgentJobs.Add(newSqlAgentJobDimRow);
                                    ctx.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DimSqlAgentJob::Update", ex);
            }
        }

        private static bool DataChanged(DimSqlAgentJob dwSqlAgentJob, SqlAgentJob domainSqlAgentJob)
        {
            return dwSqlAgentJob.Name != domainSqlAgentJob.Name ||
                   dwSqlAgentJob.Status != domainSqlAgentJob.Status.Enum.ToString();
        }
    }
}