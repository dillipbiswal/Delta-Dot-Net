using System;

namespace Datavail.Delta.RestServices.Models
{
    public class SqlAgentJobModel : ModelBase
    {
        public Guid Id { get; set; }
        public Guid DatabaseInstanceId { get; set; }
        public int StatusId { get; set; }
        public string Name { get; set; }
    }
}