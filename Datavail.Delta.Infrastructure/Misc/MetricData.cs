using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Datavail.Delta.Application
{
    public class MetricData
    {
        public Guid MetricId { get; set; }
        public Guid MetricInstanceId { get; set; }

        public List<MetricDataItem> Data { get; set; }

        public MetricData()
        {
            Data = new List<MetricDataItem>();
        }
    }
}
