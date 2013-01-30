using System;

namespace Datavail.Delta.Infrastructure.Agent.Models
{
    public class ConfigModel
    {
        public DateTime Timestamp { get; set; }
        public string GeneratingServer { get; set; }
        public string Configuration { get; set; }
    }
}