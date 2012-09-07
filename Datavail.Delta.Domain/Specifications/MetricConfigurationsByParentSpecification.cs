using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class MetricConfigurationsByParentSpecification : Specification<MetricConfiguration>
    {
        public MetricConfigurationsByParentSpecification(Guid parentIdToMatch)
            : base(
                m =>
                m.ParentCustomer.Id.Equals(parentIdToMatch) || m.ParentMetric.Id.Equals(parentIdToMatch) ||
                m.ParentMetricInstance.Id.Equals(parentIdToMatch) || m.ParentServer.Id.Equals(parentIdToMatch) ||
                m.ParentServerGroup.Id.Equals(parentIdToMatch) || m.ParentTenant.Id.Equals(parentIdToMatch))
        {
        }
    }
}