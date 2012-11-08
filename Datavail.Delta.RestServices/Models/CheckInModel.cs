using System;

namespace Datavail.Delta.RestServices.Models
{
    public class CheckInModel
    {
        public DateTime TimeStamp { get; set; }
        public Guid TenantId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string AgentVersion { get; set; }
        public Guid? CustomerId { get; set; }
    }
}