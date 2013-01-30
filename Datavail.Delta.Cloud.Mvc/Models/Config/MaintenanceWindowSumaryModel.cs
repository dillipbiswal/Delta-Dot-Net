using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MaintenanceWindowSumaryModel : TabListModel
    {
        public MaintenanceWindowParentType ParentType { get; set; }
        public Guid ParentId { get; set; }
        public TableModel MaintenanceWindowTable { get; set; }
        public string ParentName { get; set; }

        public MaintenanceWindowSumaryModel(MaintenanceWindowParentType parentType, Guid parentId, string parentName)
        {
            ParentType = parentType;
            ParentId = parentId;
            ParentName = parentName;
            ViewName = "MaintenanceWindowSummary";
        }

        public MaintenanceWindowSumaryModel()
        {
            ViewName = "MaintenanceWindowSummary";
        }
    }
}