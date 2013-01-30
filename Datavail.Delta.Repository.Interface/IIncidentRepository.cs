using System;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Repository.Interface
{
    public interface IIncidentRepository : IRepository
    {
        bool HasOpenIncident(Guid metricInstanceId);
        bool HasOpenIncident(Guid metricInstanceId, string additionalData);
    }
}