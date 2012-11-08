using System;

namespace Datavail.Delta.RestServices.Models
{
    public class MetricModel : ModelBase
    {
        public Guid Id { get; set; }
        public string AdapterAssembly { get; set; }
        public string AdapterVersion { get; set; }
        public string AdapterClass { get; set; }
        public string Name { get; set; }
        public int DatabaseVersionId { get; set; }
        public int MetricTypeId { get; set; }
        public int MetricThresholdTypeId { get; set; }
        public int StatusId { get; set; }
    }
}