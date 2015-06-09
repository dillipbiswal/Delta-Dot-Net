using System;
using System.Data.Entity;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Repository.Interface;

namespace Datavail.Delta.Repository.EfWithMigrations
{
    public class IncidentRepository : GenericRepository, IIncidentRepository
    {
        public IncidentRepository(DbContext context, IDeltaLogger logger)
            : base(context, logger)
        {
        }

        public bool HasOpenIncident(Guid metricInstanceId)
        {
            var openIncidents = Find<IncidentHistory>(h => h.MetricInstance.Id == metricInstanceId && h.CloseTimestamp.Equals(null));
            var hasOpenIncidents = openIncidents.Any();
            return hasOpenIncidents;
        }

        public bool HasOpenIncident(Guid metricInstanceId, string additionalData)
        {
            if (additionalData.Contains("LastMatchingLine")) //LastMatchingLine in additional data is only present in Logwatcher.This condition is added to fix bug 54-Logwatcher not Creating Tickets
            {
                var openIncidents = Find<IncidentHistory>(h => h.MetricInstance.Id == metricInstanceId && h.AdditionalData == additionalData && h.CloseTimestamp.Equals(null));
                var hasOpenIncidents = openIncidents.Any();
                return hasOpenIncidents;
            }
            else
            {
                var openIncidents = Find<IncidentHistory>(h => h.MetricInstance.Id == metricInstanceId && h.AdditionalData == additionalData);
                var hasOpenIncidents = openIncidents.Any();
                return hasOpenIncidents;
            }            

        }
    }
}
