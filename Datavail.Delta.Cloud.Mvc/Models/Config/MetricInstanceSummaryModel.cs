using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricInstanceSummaryModel : TabListModel
    {
        public MetricInstanceParentType ParentType { get; set; }
        public Guid ParentId { get; set; }
        public TableModel MetricInstanceTable { get; set; }
        public string ParentName { get; set; }

        public MetricInstanceSummaryModel(MetricInstanceParentType parentType, Guid parentId, string parentName)
        {
            ParentType = parentType;
            ParentId = parentId;
            ParentName = parentName;
            ViewName = "MetricInstanceSummary";
        }

         public MetricInstanceSummaryModel()
        {
            ViewName = "MetricInstanceSummary";
        }
    }
}