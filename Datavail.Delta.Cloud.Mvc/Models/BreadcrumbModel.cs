using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class BreadcrumbModel
    {
        public string BreadcrumbModelId { get; set; }
        public List<BreadcrumbItemModel> BreadcrumbItems { get; set; }

        public BreadcrumbModel()
        {
            BreadcrumbItems = new List<BreadcrumbItemModel>();
        }
    }

    public class BreadcrumbItemModel
    {
        public string Address { get; set; }
        public string Title { get; set; }
        public string IconUrl { get; set; }
    }
}