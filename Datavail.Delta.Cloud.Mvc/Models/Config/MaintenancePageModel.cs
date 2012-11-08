using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class HierarchyModel : ContentPageModel
    {
        public string SearchPlaceholder { get; set; }
        public string SelectMessage { get; set; }
        public bool IsSearchEnabled { get; set; }
    }
}