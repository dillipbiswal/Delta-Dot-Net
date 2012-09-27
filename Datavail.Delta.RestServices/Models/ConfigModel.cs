using System;

namespace Datavail.Delta.RestServices.Models
{
    public class ConfigModel
    {
        public DateTime Timestamp { get; set; }
        public string GeneratingServer { get; set; }
        public string Configuration { get; set; }
    }
}