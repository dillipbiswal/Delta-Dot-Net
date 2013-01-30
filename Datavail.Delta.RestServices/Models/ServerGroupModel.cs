using System;

namespace Datavail.Delta.RestServices.Models
{
    public class ServerGroupModel : ModelBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ParentId { get; set; }
        public int Priority { get; set; }
        public int Status { get; set; }
    }
}