using System;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Repository.Interface
{
    public interface IServerRepository : IRepository
    {
        MetricInstance GetMetricInstanceById(Guid metricInstanceId);
        void DeleteAllMetricInstances(Guid serverId);
    }
}