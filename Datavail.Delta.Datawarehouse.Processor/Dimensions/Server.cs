using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Datavail.Delta.Datawarehouse.Processor.Dimensions
{
    public class Server : DimensionBase
    {
        [Key]
        public int ServerKey { get; set; }
        public Guid ServerId { get; set; }

        public int CustomerKey { get; set; }
        public Guid CustomerId { get; set; }

        public string AgentVersion { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public bool IsVirtual { get; set; }
        public string Status { get; set; }
    }
}
