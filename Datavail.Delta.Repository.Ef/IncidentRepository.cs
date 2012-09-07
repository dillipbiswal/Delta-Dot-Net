using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.Ef.Infrastructure;
using Datavail.Delta.Repository.Interface;

namespace Datavail.Delta.Repository.Ef
{
    public class IncidentRepository : GenericRepository, IIncidentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRepository"/> class.
        /// </summary>
        public IncidentRepository(IDeltaLogger logger)
            : base(string.Empty, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRepository"/> class.
        /// </summary>
        /// <param name="connectionStringName">Name of the connection string.</param>
        /// <param name="logger"></param>
        public IncidentRepository(string connectionStringName, IDeltaLogger logger)
            : base(connectionStringName, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerRepository"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger"></param>
        public IncidentRepository(DbContext context, IDeltaLogger logger)
            : base(context, logger)
        {
        }

        public bool HasOpenIncident(Guid metricInstanceId)
        {
            var openIncidents = Find<IncidentHistory>(h => h.MetricInstance.Id == metricInstanceId && h.CloseTimestamp== null);
            var hasOpenIncidents = openIncidents.Any();
            return hasOpenIncidents;
        }
    }
}
