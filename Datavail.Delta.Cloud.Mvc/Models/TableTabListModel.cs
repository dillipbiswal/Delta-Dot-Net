using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class TableTabListModel : TabListModel
    {
        public List<ToolbarItemModel> ToolbarItems { get; set; }
        public TableModel Table { get; set; }
        public bool IsSearchEnabled { get; set; }
        public string Name { get; set; }

        public TableTabListModel()
        {
            Table = new TableModel();
            ViewName = "TableTabList";
        }
    }
}