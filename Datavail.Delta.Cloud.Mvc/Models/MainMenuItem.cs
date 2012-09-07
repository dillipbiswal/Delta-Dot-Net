using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class MainMenuItem
    {
        public string ItemId {get; set;}
        public string ItemUrl {get; set;}
        public string ItemTitle {get; set;}
        public string ItemIconUrl {get; set;}
        public string ItemName {get; set;}
        public string ItemAlt { get; set; }
        public string Class { get; set; }
        public bool IsTopLevelItem { get; set; }
        public List<MainMenuItem> ChildItems { get; set; }

        public MainMenuItem()
        {
            Class = "mainmenu-item";
            IsTopLevelItem = true;
            ChildItems = new List<MainMenuItem>();
        }
    }
}