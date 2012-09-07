using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure;
using System.Web.Mvc;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricDataItemModel
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public string TagName { get; set; }
        public string Value { get; set; }
        public Constants.MetricDataRenderType RenderType { get; set; }
        public Dictionary<string, string> ValueOptions { get; set; }
        public string SelectedValueOption { get; set; }
        public List<MetricDataItemModel> Children { get; set; }
        public bool IsRequired { get; set; }

        public MetricDataItemModel()
        {
            //Text by default
            RenderType = Constants.MetricDataRenderType.Text;
            Children = new List<MetricDataItemModel>();
            ValueOptions = new Dictionary<string, string>();
            Value = string.Empty;
        }
    }
}
