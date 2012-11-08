using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace Datavail.Delta.Agent.SharedCode.Common
{
    public class Services
    {
        public static void GetSqlInstanceList()
        {
            var services = ServiceController.GetServices();

            foreach (var service in services.Where(service => service.DisplayName.Contains("SQL Server")))
            {
                const string regexexp = @"(?<a>\()(?<value>[^)]*?)(?<-a>\))";
                var regex = new Regex(regexexp);

                var status = service.Status;
                var instanceName = regex.Match(service.DisplayName).Groups[2].Value;
            }
        }
    }
}
