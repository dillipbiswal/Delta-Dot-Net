using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServersByCustomerIdNotCheckedInSpecification : Specification<Server>
    {
        public ServersByCustomerIdNotCheckedInSpecification(Guid customerIdToMatch, int minutes)
            : base(p => p.Customer.Id == customerIdToMatch && DateTime.UtcNow - p.LastCheckIn > TimeSpan.FromMinutes(minutes))
        {
        }
    }
}
