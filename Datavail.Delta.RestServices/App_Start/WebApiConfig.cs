using System.Web.Http;

namespace Datavail.Delta.RestServices
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
           

            config.Routes.MapHttpRoute(
                 name: "CheckIn",
                 routeTemplate: "v41/Server/CheckIn/{id}",
                 defaults: new { controller = "Server", action = "CheckIn" });

            config.Routes.MapHttpRoute(
                name: "Config",
                routeTemplate: "v41/Server/Config/{id}",
                defaults: new { controller = "Server", action = "GetConfig" });

            config.Routes.MapHttpRoute(
                name: "AssemblyList",
                routeTemplate: "v41/Server/AssemblyList/{id}",
                defaults: new { controller = "Server", action = "GetAssemblyList" });

            config.Routes.MapHttpRoute(
               name: "PostData",
               routeTemplate: "v41/Server/PostData/{id}",
               defaults: new { controller = "Server", action = "PostData" });

            config.Routes.MapHttpRoute(
               name: "AssemblyDownload",
               routeTemplate: "v41/Assembly/{name}/{version}",
               defaults: new { controller = "Assembly" });

            

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "v41/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
