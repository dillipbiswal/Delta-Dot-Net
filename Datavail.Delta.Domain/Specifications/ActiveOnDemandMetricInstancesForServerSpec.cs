using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ActiveOnDemandMetricInstancesForServerSpec : Specification<OnDemandMetricInstance>
    {
        public ActiveOnDemandMetricInstancesForServerSpec(Guid serverId)
            : base(
                mi => mi.Server.Id.Equals(serverId) &&
                      mi.Status == Status.Active
                && mi.Metric.Status == Status.Active
                && (mi.DatabaseInstance == null || mi.DatabaseInstance.Status == Status.Active) &&
                (mi.Database == null || mi.Database.Status == Status.Active && mi.Database.Instance.Status == Status.Active))
        {
        }
    }
}
