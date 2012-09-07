using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Models.Config;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class DeltaConfigPortalModel
    {
        public List<MainMenuItem> MainMenuItems { get; set; }
        public ContentPageModel ContentPageModel { get; set; }
        public string ContentPageView { get; set; }
        public string Title { get; set; }
        public string LoginMessage { get; set; }
        public string LogOffUrl { get; set; }
        public string LogOffTitle { get; set; }
        public string LogOffImageUrl { get; set; }
        public string FooterUrl { get; set; }

        public DeltaConfigPortalModel()
        {
            MainMenuItems = new List<MainMenuItem>();
        }
    }
}