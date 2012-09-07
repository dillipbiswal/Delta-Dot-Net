using System.Web.Mvc;
using System.Web.Routing;
using Datavail.Delta.Cloud.Mvc.RouteConstraints;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.Startup
{
	public class RouteBootstrapper : IRunAtStartup
	{
		private readonly RouteCollection _routes;

		public RouteBootstrapper(RouteCollection routes)
		{
			_routes = routes;
		}

		public void Execute()
		{
			_routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            _routes.MapRoute("LogOn",
                            "LogOn",
                            new { controller = "Authentication", action = "LogOn" });

			_routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
                new { controller = "Config", action = "Maintenance", id = "1A19A18A-846C-49DA-93C1-8948AFDC0151" }, //Route defaults
                new { id = new GuidConstraint() } // Route Constraints
            );
		}
	}
}