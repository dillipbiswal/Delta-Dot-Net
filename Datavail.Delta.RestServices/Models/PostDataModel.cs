using System;

namespace Datavail.Delta.RestServices.Models
{
    public class PostDataModel
    {
        public DateTime Timestamp { get; set; }
        public Guid TenantId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string Data { get; set; }
    }
}