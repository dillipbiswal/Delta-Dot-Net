using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.Linq;
using Datavail.Delta.Infrastructure.Azure;
using Datavail.Framework.Azure.Configuration;
using Datavail.Framework.Azure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Datavail.Delta.Cloud.Ws
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            SetupDiagnostics();
            RoleEnvironment.Changed += (RoleEnvironment_Changed);
            return base.OnStart();
        }

        private static void SetupDiagnostics()
        {
            var configuration = new AzureDiagnoticsConfiguration(DiagnosticMonitor.GetDefaultInitialConfiguration())
                                    {
                                        AzureLogTransferTime = TimeSpan.FromMinutes(1),
                                        SetupLog4Net = true
                                    };

            var counterCreationData = new List<CounterCreationData>
                                          {
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.CollectionServiceMessagesQueued,
                                                      CounterHelp = AzureConstants.PerfCounters.CollectionServiceMessagesQueuedDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.CollectionServiceMessagesIgnored,
                                                      CounterHelp = AzureConstants.PerfCounters.CollectionServiceMessagesIgnoredDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.CollectionServiceMessagesForTest,
                                                      CounterHelp = AzureConstants.PerfCounters.CollectionServiceMessagesForTestDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.CheckInServiceCheckIns,
                                                      CounterHelp = AzureConstants.PerfCounters.CheckInServiceCheckInsDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.UpdateServiceConfigurationDownloads,
                                                      CounterHelp = AzureConstants.PerfCounters.UpdateServiceConfigurationDownloadsDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.UpdateServiceAssemblyDownloads,
                                                      CounterHelp = AzureConstants.PerfCounters.UpdateServiceAssemblyDownloadsDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  }
                                          };


            configuration.AddPerformanceCounterCategory(AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.CountersCategoryDesc, counterCreationData);

            var counters = new[]
                               {
                                   new CounterTransferConfig(@"\Processor(*)\% Processor Time", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\.NET CLR Exceptions(_Global_)\# Exceps Thrown / sec", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\Process(" + Process.GetCurrentProcess().ProcessName + @")\Working Set", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\LogicalDisk(*)\Disk Bytes/sec", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\ASP.NET(*)\Requests Current", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\ASP.NET(*)\Requests Queued", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\ASP.NET(*)\Requests Rejected", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.CollectionServiceMessagesQueued), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.CollectionServiceMessagesIgnored), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.CollectionServiceMessagesForTest), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.CheckInServiceCheckIns), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.UpdateServiceConfigurationDownloads), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.UpdateServiceAssemblyDownloads), TimeSpan.FromSeconds(1)),
                               };

            configuration.CountersToTransfer.AddRange(counters);
            var config = configuration.Configure();
            DiagnosticMonitor.Start(configuration.StorageConnectionStringKey, config);
        }

        private void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            if (e.Changes.Any(chg => chg is RoleEnvironmentTopologyChange))
            {
                // Perform an action, for example, you can initialize a client, 
                // or you can recycle the role
            }
        }
    }
}