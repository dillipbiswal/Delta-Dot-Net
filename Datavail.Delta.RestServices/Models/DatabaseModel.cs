using System;

namespace Datavail.Delta.RestServices.Models
{
    public class DatabaseModel : ModelBase
    {
        public Guid Id { get; set; }
        public Guid DatabaseInstanceId { get; set; }
        public int StatusId { get; set; }
        public string Name { get; set; }
    }
}