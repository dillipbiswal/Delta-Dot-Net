using System;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Facts
{
    public abstract class InstanceScopedFact
    {
        [Key]
        public int FactKey { get; set; }
        public int DateKey { get; set; }
        public string TimeKey { get; set; }
        public int TenantKey { get; set; }
        public Guid TenantId { get; set; }
        public int CustomerKey { get; set; }
        public Guid? CustomerId { get; set; }
        public int ServerKey { get; set; }
        public Guid ServerId { get; set; }
        public int InstanceKey { get; set; }
        public Guid InstanceId { get; set; }
    }
}