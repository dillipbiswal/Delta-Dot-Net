using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Domain;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricSelectModel
    {
        [Display(Name = "Metric")]
        public SelectList MetricId { get; set; }

        [HiddenInput(DisplayValue = false)]
        public string MetricSelectFormAction { get; set; }

        public MetricSelectModel(IEnumerable<Metric> metrics)
        {
            MetricId = new SelectList(metrics, "Id", "Name");
            MetricId.OrderBy(x => x.Text);
        }

    }
}