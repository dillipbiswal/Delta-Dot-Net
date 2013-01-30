using System.Web;
using System.Web.Mvc;

namespace Datavail.Delta.RestServices
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}