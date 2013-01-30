using System;

namespace Datavail.Delta.RestServices.Models
{
    public class TenantModel : ModelBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int StatusId { get; set; }
    }
}