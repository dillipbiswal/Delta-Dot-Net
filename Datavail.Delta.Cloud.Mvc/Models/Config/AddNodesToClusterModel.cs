using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class AddNodesToClusterModel
    {
        public string DragId { get; set; }
        public string DropId { get; set; }
        public string Hostname { get; set; }
        public string Clustername { get; set; }
    }
}