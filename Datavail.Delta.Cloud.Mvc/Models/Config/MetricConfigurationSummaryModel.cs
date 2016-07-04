using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricConfigurationSummaryModel : TabListModel
    {
        public IEnumerable<Metric> Metrics { get; set; }

        [Display(Name = "Metric: ")]
        public Guid MetricId { get; set; }

        public MetricConfigurationParentType ParentType { get; set; }

        public Guid ParentId { get; set; }

        public MetricConfigurationModel MetricConfig { get; set; }

        public string ParentName { get; set; }

        public MetricConfigurationSummaryModel(MetricConfigurationParentType parentType, Guid parentId, string parentName)
        {
            ParentType = parentType;
            ParentId = parentId;
            ParentName = parentName;
            ViewName = "MetricConfigurationSummary";
        }

        public MetricConfigurationSummaryModel()
        {

        }
    }
}