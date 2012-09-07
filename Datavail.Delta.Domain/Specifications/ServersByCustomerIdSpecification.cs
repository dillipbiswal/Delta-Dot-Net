using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServersByCustomerIdSpecification : Specification<Server> 
    {
        public ServersByCustomerIdSpecification(Guid customerIdToMatch)
            : base(p => p.Customer.Id==customerIdToMatch)
        {
        }
    }
}
