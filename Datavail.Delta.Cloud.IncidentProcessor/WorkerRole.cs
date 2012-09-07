using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.Infrastructure.Azure;
using Datavail.Delta.Infrastructure.Azure.QueueMessage;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.Ef;
using Datavail.Delta.Repository.Ef.Infrastructure;
using Datavail.Delta.Repository.Interface;
using Datavail.Framework.Azure.Cache;
using Datavail.Framework.Azure.Configuration;
using Datavail.Framework.Azure.Queue;
using Datavail.Framework.Azure.WorkerRole;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Datavail.Delta.Cloud.IncidentProcessor
{
    public class WorkerRole : ThreadedRoleEntryPoint
    {
        private IUnityContainer _container;
        private WorkerEntryPoint[] _workers;

        public override bool OnStart()
        {
            Trace.WriteLine(string.Format("WorkerRole::OnStart called/{0}/{1}", RoleEnvironment.CurrentRoleInstance.Id, Thread.CurrentThread.ManagedThreadId));
            SetupDiagnostics();

            RoleEnvironment.Changed += (RoleEnvironment_Changed);

            var storage = new ThreadDbContextStorage();
            DbContextInitializer.Instance().InitializeObjectContextOnce(() =>
                                                                            {
                                                                                DbContextManager.InitStorage(storage);
                                                                                DbContextManager.Init("DeltaConnectionString", new[] { Environment.CurrentDirectory + "\\Datavail.Delta.Repository.Ef.dll" });
                                                                            });
            RegisterTypes();

            return StartWorkers();
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
                                                      CounterName = AzureConstants.PerfCounters.IncidentProcessorMessagesProcessed,
                                                      CounterHelp = AzureConstants.PerfCounters.IncidentProcessorMessagesProcessedDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.IncidentProcessorIncidentsOpened,
                                                      CounterHelp = AzureConstants.PerfCounters.IncidentProcessorIncidentsOpenedDesc,
                                                      CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                                                  },
                                              new CounterCreationData
                                                  {
                                                      CounterName = AzureConstants.PerfCounters.IncidentProcessorQueueDepth,
                                                      CounterHelp = AzureConstants.PerfCounters.IncidentProcessorQueueDepthDesc,
                                                      CounterType = PerformanceCounterType.NumberOfItems32
                                                  }
                                          };


            configuration.AddPerformanceCounterCategory(AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.CountersCategoryDesc, counterCreationData);

            var counters = new[]
                               {
                                   new CounterTransferConfig(@"\Processor(*)\% Processor Time", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\.NET CLR Exceptions(_Global_)\# Exceps Thrown / sec", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\Process(" + Process.GetCurrentProcess().ProcessName + @")\Working Set", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(@"\Processor(*)\% Processor Time", TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorMessagesProcessed), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorIncidentsOpened), TimeSpan.FromSeconds(1)),
                                   new CounterTransferConfig(string.Format(@"\{0}(*)\{1}", AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorQueueDepth), TimeSpan.FromSeconds(1)),
                               };

            configuration.CountersToTransfer.AddRange(counters);
            var config = configuration.Configure();

            DiagnosticMonitor.Start(configuration.StorageConnectionStringKey, config);
        }

        private bool StartWorkers()
        {
            if (_workers == null)
            {
                var workers = new List<WorkerEntryPoint>();
                var numberOfWorkers = Int32.Parse(RoleEnvironment.GetConfigurationSettingValue("NumberOfWorkerThreads"));

                for (var i = 0; i < numberOfWorkers; i++)
                {
                    var processor = _container.Resolve<IncidentProcessorWorker>();
                    workers.Add(processor);
                }

                _workers = workers.ToArray();
            }

            return OnStart(_workers);
        }

        private void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            if (e.Changes.Any(chg => chg is RoleEnvironmentTopologyChange))
            {
                // Perform an action, for example, you can initialize a client, 
                // or you can recycle the role

            }
        }

        public void RegisterTypes()
        {
            _container = new UnityContainer();

            var account = AzureConfiguration.GetStorageAccount("Datavail.Framework.StorageConnectionString");
            _container.RegisterInstance(account);

            _container.RegisterInstance(_container);

            //Common
            _container.RegisterType<IDeltaLogger, DeltaLogger>();
            _container.RegisterType<IAzureCache, AzureCache>();

            //Repositories
            _container.RegisterType<DbContext, DbContext>(new PerThreadLifetimeManager(), new InjectionFactory(f => DbContextManager.CurrentFor("DeltaConnectionString")));
            //_container.RegisterType<DbContext, DeltaConnectionString>(new PerThreadLifetimeManager());
            _container.RegisterType<IRepository, GenericRepository>(new InjectionConstructor("DeltaConnectionString", _container.Resolve<IDeltaLogger>()));
            _container.RegisterType<IServerRepository, ServerRepository>(new InjectionConstructor("DeltaConnectionString", _container.Resolve<IDeltaLogger>()));

            //Application Facades
            _container.RegisterType<IIncidentService, IncidentService>();
            _container.RegisterType<IServerService, ServerService>();
            _container.RegisterType<IServiceDesk, ServiceDesk>();

            //Queues
            _container.RegisterType<IAzureQueue<CheckInMessage>, PartitionedAzureQueue<CheckInMessage>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.CheckIns));
            _container.RegisterType<IAzureQueue<DataCollectionMessage>, PartitionedAzureQueue<DataCollectionMessage>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.Collections));
            _container.RegisterType<IAzureQueue<DataCollectionMessageWithError>, PartitionedAzureQueue<DataCollectionMessageWithError>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.CollectionErrors));
            _container.RegisterType<IAzureQueue<DataCollectionArchiveMessage>, PartitionedAzureQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.CollectionArchives));
        }
    }
}