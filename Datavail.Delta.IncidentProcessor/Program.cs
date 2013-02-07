using System.ServiceProcess;

namespace Datavail.Delta.IncidentProcessor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[] { new IncidentProcessor() };
            ServiceBase.Run(servicesToRun);
        }
    }
}
