using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Datavail.Delta.IncidentProcessor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            Debugger.Launch();

            var servicesToRun = new ServiceBase[] 
                                              { 
                                                  new IncidentProcessor() 
                                              };
            ServiceBase.Run(servicesToRun);
        }
    }
}
