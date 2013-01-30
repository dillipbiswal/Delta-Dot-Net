using System;

namespace Datavail.Delta.RestServices.Models
{
    public class CustomerModel : ModelBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int StatusId { get; set; }
        public Guid TenantId { get; set; }
    }
}