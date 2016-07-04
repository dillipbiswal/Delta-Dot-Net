using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Infrastructure;

namespace Datavail.Delta.Cloud.Mvc.Controllers
{
    [Authorize(Roles = Constants.DELTAADMIN)]
    public class FAQController : DeltaController
    {
        [HttpGet]
        public ActionResult FAQ()
        {
            return View();
        }

    }
}
