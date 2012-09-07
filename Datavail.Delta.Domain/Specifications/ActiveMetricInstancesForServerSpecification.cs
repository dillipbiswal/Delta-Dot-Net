using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ActiveMetricInstancesForServerSpecification : Specification<MetricInstance>
    {
        public ActiveMetricInstancesForServerSpecification(Guid serverId)
            : base(
                mi => mi.Server.Id.Equals(serverId) &&
                      mi.Status.Value.Equals((int) Status.Active) &&
                      mi.Metric.Status.Value.Equals((int) Status.Active) &&
                      (mi.DatabaseInstance == null || mi.DatabaseInstance.Status.Value.Equals((int) Status.Active)) &&
                      (mi.Database == null || mi.Database.Status.Value.Equals((int) Status.Active) && mi.Database.Instance.Status.Value.Equals((int)Status.Active))
                ) { }
    }
}

//(mi.DatabaseInstance == null || mi.DatabaseInstance.Status == Status.Active) &&
//                      (mi.Database == null || mi.Database.Status == Status.Active) &&
