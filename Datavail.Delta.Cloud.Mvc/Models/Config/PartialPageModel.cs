using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class PartialPageModel : ContentPageModel
    {
        public TabModel Tabs { get; set; }

        public string ParentId { get; set; }
        public string ParentName { get; set; }
        public string ParentType { get; set; }
        public string BreadcrumbAddress { get; set; }
        public string BreadcrumbTitle { set; get; }

        public PartialPageModel(string tabId, bool showBreadCrumb, string partialPageView, string mainMenuItemId)
        {
            Tabs = new TabModel(tabId);
            ShowBreadCrumb = showBreadCrumb;
            PartialPageView = partialPageView;
            MainMenuItemId = mainMenuItemId;
        }
    }
}