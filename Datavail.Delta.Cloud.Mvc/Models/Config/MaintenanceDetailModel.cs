using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class ItemDetailModel
    {
        public TabModel Details { get; set; }

        public ItemDetailModel(string tabId)
        {
            Details = new TabModel(tabId);
        }
    }
}