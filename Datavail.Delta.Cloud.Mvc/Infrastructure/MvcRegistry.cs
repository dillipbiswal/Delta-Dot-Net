using System.Security.Principal;
using System.Web;
using System.Web.Routing;
using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
    public class MvcRegistry : Registry
    {
        public MvcRegistry()
        {
            For<RouteCollection>().Use(RouteTable.Routes);
            For<IIdentity>().Use(() => HttpContext.Current.User.Identity);
        }
    }
}