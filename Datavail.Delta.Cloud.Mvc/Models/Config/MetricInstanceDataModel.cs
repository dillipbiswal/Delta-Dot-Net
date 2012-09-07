using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Infrastructure;
using System.ComponentModel;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricInstanceDataModel
    {
        [HiddenInput(DisplayValue=false)]
        public Guid MetricInstanceId { get; set; }
        
        [HiddenInput(DisplayValue = false)]
        public Guid MetricId { get; set; }

        [HiddenInput(DisplayValue = false)]
        public Guid MetricInstanceParentId { get; set; }

        [UIHint("MetricDataItems")]
        public List<MetricDataItemModel> DataItems { get; set; }

        [UIHint("FilteredStatusDropDown")]
        public Status Status { get; set; }

        [HiddenInput(DisplayValue = false)]
        public string MetricInstanceDataFormAction { get; set; }

        public MetricInstanceDataModel()
        {
            DataItems = new List<MetricDataItemModel>();
        }
    }
}