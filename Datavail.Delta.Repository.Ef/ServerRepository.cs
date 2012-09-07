using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Transactions;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.Ef.Infrastructure;
using Datavail.Delta.Repository.Interface;
using System.Linq;

namespace Datavail.Delta.Repository.Ef
{
    public class ServerRepository : GenericRepository, IServerRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRepository"/> class.
        /// </summary>
        public ServerRepository(IDeltaLogger logger)
            : base(string.Empty, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRepository"/> class.
        /// </summary>
        /// <param name="connectionStringName">Name of the connection string.</param>
        /// <param name="logger"></param>
        public ServerRepository(string connectionStringName, IDeltaLogger logger)
            : base(connectionStringName, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRepository"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger"></param>
        public ServerRepository(DbContext context, IDeltaLogger logger)
            : base(context,logger)
        {
        }

        public MetricInstance GetMetricInstanceById(Guid metricInstanceId)
        {
            var criteria = new Specification<MetricInstance>(mi => mi.Id == metricInstanceId);
            var metricInstance = criteria.SatisfyingEntityFrom(GetQuery<MetricInstance>().AsNoTracking());

            return metricInstance;

        }


        public void DeleteAllMetricInstances(Guid serverId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                var metricInstances = Context.Set<MetricInstance>().Where(m => m.Server.Id == serverId);

                foreach (var metricInstance in metricInstances.ToList())
                {
                    Context.Set<MetricInstance>().Remove(metricInstance);
                }

                UnitOfWork.SaveChanges();
            }
        }
    }
}
