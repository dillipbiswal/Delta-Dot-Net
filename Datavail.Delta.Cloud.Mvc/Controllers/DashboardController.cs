using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Datavail.Delta.Cloud.Mvc.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        //
        // GET: /Dashboard/

        public ActionResult Overview()
        {
            if (Request.IsAjaxRequest())
            {
                return PartialView();
            }

            return View();
        }

    }
}
