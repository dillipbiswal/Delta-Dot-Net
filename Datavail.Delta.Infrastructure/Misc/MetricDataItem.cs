using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Datavail.Delta.Application
{
    public class MetricDataItem
    {
        public string DisplayName { get; set; }
        public string TagName { get; set; }
        public string Value { get; set; }
        public List<MetricDataItem> Children { get; set; }
        public Dictionary<string, string> ValueOptions { get; set; }
        public string SelectedValueOption { get; set; }
        public bool MultipleValues { get; set; }
        public bool IsRequired { get; set; }

        public MetricDataItem()
        {
            Children = new List<MetricDataItem>();
            ValueOptions = new Dictionary<string, string>();
        }
    }
}
