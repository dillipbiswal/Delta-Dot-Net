using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServersByTenantAndStatusSpecification : Specification<Server> 
    {
        public ServersByTenantAndStatusSpecification(Guid tenantId, Status statusToMatch)
            : base(p => p.Tenant.Id==tenantId && p.Status == statusToMatch)
        {
        }
    }
}
