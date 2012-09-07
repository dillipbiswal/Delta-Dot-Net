using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class ServersByStatusSpecification : Specification<Server> 
    {
        public ServersByStatusSpecification(Status statusToMatch)
            : base(p => p.Status.Value == (int)statusToMatch)
        {
        }
    }
}
