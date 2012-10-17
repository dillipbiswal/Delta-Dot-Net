using System;

namespace Datavail.Delta.RestServices.Models
{
    public class ServerModel : ModelBase
    {
        public Guid Id { get; set; }
        public string AgentVersion { get; set; }
        public Guid CustomerId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public bool IsVirtual { get; set; }
        public DateTime LastCheckIn { get; set; }
        public int StatusId { get; set; }
        public Guid TenantId { get; set; }
    }
}