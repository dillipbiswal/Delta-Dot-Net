using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

namespace Datavail.Delta.IncidentProcessor.ConsoleTester
{
    class Program
    {
        private static IUnityContainer _container;
        private static Thread[] _workerThreads;
        private static WorkerBase[] _workers;
        private const int NUMBER_OF_WORKER_THREADS = 25;
        private static int _totalNumberOfThreads;

        static void Main(string[] args)
        {
            BootstrapIoc();

            _totalNumberOfThreads = NUMBER_OF_WORKER_THREADS + 3;

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

            var firstWorkerThread = _totalNumberOfThreads - NUMBER_OF_WORKER_THREADS;
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

        private static void BootstrapIoc()
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
