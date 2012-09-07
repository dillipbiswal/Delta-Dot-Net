using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class ContentPageModel
    {
        public List<ToolbarItemModel> ToolbarItems { get; set; }
        public List<ContextMenuItemModel> ContextMenuItems { get; set; }
        public TableModel Table { get; set; }

        public string MainMenuItemId { get; set; }
        public bool ShowBreadCrumb { get; set; }
        public string PartialPageView { get; set; }
        public string ContentPageView { get; set; }
        public string PageId { get; set; }
        public string PageHeader { get; set; }

        public ContentPageModel()
        {
            ToolbarItems = new List<ToolbarItemModel>();
            ContextMenuItems = new List<ContextMenuItemModel>();
        }
    }
}