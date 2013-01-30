using System;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Domain.Specifications
{
    public class MetricInstancesByServerSpecification : Specification<MetricInstance> 
    {
        public MetricInstancesByServerSpecification(Guid serverIdToMatch)
            : base(m=>m.Server.Id == serverIdToMatch)
        {
        }
    }
}
