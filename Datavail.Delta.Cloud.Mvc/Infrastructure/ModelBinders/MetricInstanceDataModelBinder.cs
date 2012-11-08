using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Models.Config;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ModelBinders
{
    public class MetricInstanceDataModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var metricInstanceModelBinder = new MetricInstanceDataModel();
            

            return metricInstanceModelBinder;
        }
    }
}