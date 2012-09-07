using System;
using System.Collections.Generic;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Repository.Interface
{
    public interface IMetricConfigurationRepository : IRepository
    {
        IEnumerable<MetricConfiguration> GetAllForParentId(Guid id);
    }
}