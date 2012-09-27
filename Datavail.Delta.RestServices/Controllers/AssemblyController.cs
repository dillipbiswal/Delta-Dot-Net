
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.RestServices.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Datavail.Delta.RestServices.Controllers
{
    public class AssemblyController : ApiController
    {
        private readonly IDeltaLogger _logger;

        public AssemblyController(IDeltaLogger deltaLogger)
        {
            _logger = deltaLogger;
        }
        
        [HttpGet]
        public HttpResponseMessage GetAssembly(string name, string version)
        {
            try
            {
                var path = HttpContext.Current.Server.MapPath("~/DeltaAssemblies");

                var filename = String.Format("{0}.{1}.dll", name, version.Replace("4.0.", "4.1."));
                var fullpath = Path.Combine(path, filename);

                var fs = File.OpenRead(fullpath);
                try
                {
                    var bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                    
                    var model = new AssemblyDownloadModel
                    {
                        AssemblyName = name,
                        Version = version,
                        GeneratingServer = Environment.MachineName,
                        Timestamp = DateTime.UtcNow,
                        Contents = bytes
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, model);
                }
                finally
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetAssembly (" + name + "," + version + ")", ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}
