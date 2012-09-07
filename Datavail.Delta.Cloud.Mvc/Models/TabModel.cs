using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class TabModel
    {
        public string Id { get; set; }
        public List<TabListModel> TabList { get; set; }

        public TabModel(string id)
        {
            Id = id;
            TabList = new List<TabListModel>();
        }
    }
}