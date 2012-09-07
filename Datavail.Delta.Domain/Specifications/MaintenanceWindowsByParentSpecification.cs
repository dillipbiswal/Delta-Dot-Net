using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class MaintenanceWindowsByParentSpecification : Specification<MaintenanceWindow>
    {
        public MaintenanceWindowsByParentSpecification(Guid parentIdToMatch)
            : base(
                m =>
                m.Customer.Id.Equals(parentIdToMatch) || m.Metric.Id.Equals(parentIdToMatch) ||
                m.MetricInstance.Id.Equals(parentIdToMatch) || m.Server.Id.Equals(parentIdToMatch) ||
                m.ServerGroup.Id.Equals(parentIdToMatch) || m.Tenant.Id.Equals(parentIdToMatch))
        {
        }
    }
}
