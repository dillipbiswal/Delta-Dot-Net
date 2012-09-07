using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServersByTenantIdNotCheckedInSpecification : Specification<Server> 
    {
        public ServersByTenantIdNotCheckedInSpecification(Guid tenantIdToMatch, int minutes)
            : base(p => p.Tenant.Id==tenantIdToMatch && DateTime.UtcNow - p.LastCheckIn > TimeSpan.FromMinutes(minutes))
        {
        }
    }
}
