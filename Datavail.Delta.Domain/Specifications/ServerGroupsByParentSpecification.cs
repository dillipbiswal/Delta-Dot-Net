using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServerGroupsByParentSpecification : Specification<ServerGroup>
    {
        public ServerGroupsByParentSpecification(Guid parentIdToMatch)
            : base(m => (m.ParentCustomer.Id.Equals(parentIdToMatch) || m.ParentTenant.Id.Equals(parentIdToMatch)))
        {
        }
    }
}
