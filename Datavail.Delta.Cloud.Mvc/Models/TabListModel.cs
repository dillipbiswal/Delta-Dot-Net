using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class TabListModel
    {
        public string TabListId { get; set; }
        public string TabListName { get; set; }
        public string TabListIconUrl { set; get; }
        public string ViewName { get; set; }
    }
}