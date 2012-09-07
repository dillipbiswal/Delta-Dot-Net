using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Models;
using Datavail.Delta.Cloud.Mvc.Models.Authentication;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Domain;
using System.Web.Security;
using Datavail.Delta.Application.Interface;

namespace Datavail.Delta.Cloud.Mvc.Controllers
{
    public class AuthenticationController : DeltaController
    {
        private readonly IServerService _serverService;

        public AuthenticationController(IServerService serverService)
        {
            _serverService = serverService;
        }

        [HttpGet]
        public ActionResult LogOn()
        {
            
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(LogOnForm form, string returnUrl)
        {
            var authenticated = _serverService.AuthenticateUser(form.EmailAddress, form.Password);

            if (!authenticated)
            {
                return View(new LogOnForm { EmailAddress = form.EmailAddress });
            }

            FormsAuthentication.SetAuthCookie(form.EmailAddress, true);
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/";
            }

            var redirectResult = new RedirectResult(returnUrl);

            return redirectResult;
        }

        [Authorize]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return new RedirectResult(FormsAuthentication.LoginUrl);
        }
    }
}