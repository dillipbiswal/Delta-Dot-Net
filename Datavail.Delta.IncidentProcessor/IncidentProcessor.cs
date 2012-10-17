using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Datavail.Delta.Repository.Interface;
using Microsoft.Practices.Unity;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using Database = System.Data.Entity.Database;

namespace Datavail.Delta.IncidentProcessor
{
    public partial class IncidentProcessor : ServiceBase
    {
        private IUnityContainer _container;
        Thread[] _workerThreads;
        WorkerBase[] _workers;
        private readonly int _numberOfWorkerThreads = 5;
        private readonly int _totalNumberOfThreads;

        public IncidentProcessor()
        {
            Debugger.Launch();

            Int32.TryParse(ConfigurationManager.AppSettings["NumberOfWorkerThreads"], out _numberOfWorkerThreads);
            _totalNumberOfThreads = _numberOfWorkerThreads + 3;

            BootstrapIoc();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _workers = new WorkerBase[_totalNumberOfThreads];
            _workerThreads = new Thread[_totalNumberOfThreads];

            Database.SetInitializer<DeltaDbContext>(null);

            _workers[0] = _container.Resolve<OpenIncidentWorker>();
            _workers[0].ServiceStarted = true;
            var incst = new ThreadStart(_workers[0].Run);

            _workerThreads[0] = new Thread(incst) { Name = "OpenIncidentWorker" };

            _workers[1] = _container.Resolve<CheckInWorker>();
            _workers[1].ServiceStarted = true;
            var checkst = new ThreadStart(_workers[1].Run);
            _workerThreads[1] = new Thread(checkst) { Name = "CheckInWorker" }; ;

            _workers[2] = _container.Resolve<UpdateTicketClosedWorker>();
            _workers[2].ServiceStarted = true;
            var updateTicketClosedst = new ThreadStart(_workers[2].Run);
            _workerThreads[2] = new Thread(updateTicketClosedst) { Name = "UpdateTicketClosedWorker" }; ;

            var firstWorkerThread = _totalNumberOfThreads - _numberOfWorkerThreads;
            for (var i = firstWorkerThread; i < _totalNumberOfThreads; i++)
            {
                // create an object
                _workers[i] = _container.Resolve<IncidentProcessorWorker>();

                // set properties on the object
                _workers[i].ServiceStarted = true;

                // create a thread and attach to the object
                var st = new ThreadStart(_workers[i].Run);
                _workerThreads[i] = new Thread(st) { Name = "IncidentProcessor " + i };
            }

            // start the threads
            for (var i = 0; i < _totalNumberOfThreads; i++)
            {
                _workerThreads[i].Start();
            }
        }

        protected override void OnStop()
        {
            for (var i = 0; i < _totalNumberOfThreads; i++)
            {
                // set flag to stop worker thread
                _workers[i].ServiceStarted = false;

                // give it a little time to finish any pending work
                _workerThreads[i].Join(TimeSpan.FromSeconds(1));
            }
        }

        public void BootstrapIoc()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(_container);

            //Common
            _container.RegisterType<IDeltaLogger, DeltaIncidentProcessorLogger>();

            //Repositories
            _container.RegisterType<DbContext, DeltaDbContext>(new ContainerControlledLifetimeManager());


            _container.RegisterType<IRepository, GenericRepository>(new InjectionConstructor(typeof(DbContext), _container.Resolve<IDeltaLogger>()));
            _container.RegisterType<IServerRepository, ServerRepository>(new InjectionConstructor(typeof(DbContext), _container.Resolve<IDeltaLogger>()));
            _container.RegisterType<IIncidentRepository, IncidentRepository>(new InjectionConstructor(typeof(DbContext), typeof(IDeltaLogger)));

            //Application Facades
            _container.RegisterType<IIncidentService, IncidentService>();
            _container.RegisterType<IServerService, ServerService>();
            _container.RegisterType<IServiceDesk, ServiceDesk>();

            //Queues
            _container.RegisterType<IQueue<CheckInMessage>, SqlQueue<CheckInMessage>>(new InjectionConstructor(QueueNames.CheckInQueue));
            _container.RegisterType<IQueue<DataCollectionMessage>, SqlQueue<DataCollectionMessage>>(new InjectionConstructor(QueueNames.IncidentProcessorQueue));
            _container.RegisterType<IQueue<DataCollectionArchiveMessage>, SqlQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(QueueNames.DataWarehouseQueue));
            _container.RegisterType<IQueue<DataCollectionMessageWithError>, SqlQueue<DataCollectionMessageWithError>>(new InjectionConstructor(QueueNames.ErrorQueue));
            _container.RegisterType<IQueue<DataCollectionTestMessage>, SqlQueue<DataCollectionTestMessage>>(new InjectionConstructor(QueueNames.TestQueue));
            _container.RegisterType<IQueue<OpenIncidentMessage>, SqlQueue<OpenIncidentMessage>>(new InjectionConstructor(QueueNames.OpenIncidentQueue));

            //Make a DB Call to force DbContext Initialization

            //var dbContext = _container.Resolve<DbContext>();
            //dbContext.Set<Tenant>().Any();
        }
    }
}
