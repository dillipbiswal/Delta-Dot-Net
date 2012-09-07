using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServersByTenantIdSpecification : Specification<Server> 
    {
        public ServersByTenantIdSpecification(Guid tenantIdToMatch)
            : base(p => p.Tenant.Id==tenantIdToMatch)
        {
        }
    }
}
