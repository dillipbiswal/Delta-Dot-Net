using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class ApiUriWindowSumaryModel : TabListModel
    {
        public ApiUriWindowParentType ParentType { get; set; }
        public Guid ParentId { get; set; }
        public TableModel ApiUriWindowTable { get; set; }
        public string ParentName { get; set; }

        public string PlugInName { get; set; }
        public string URIAddress { get; set; }
        public string AgentServerId { get; set; }

        public ApiUriWindowSumaryModel(ApiUriWindowParentType parentType, Guid parentId, string parentName)
        {
            ParentType = parentType;
            ParentId = parentId;
            ParentName = parentName;
            ViewName = "ApiUriWindowSummary";
        }

        public ApiUriWindowSumaryModel()
        {
            ViewName = "ApiUriWindowSummary";
        }
    }
}