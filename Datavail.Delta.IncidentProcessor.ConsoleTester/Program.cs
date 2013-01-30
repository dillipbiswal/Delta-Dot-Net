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
using Ninject;

namespace Datavail.Delta.IncidentProcessor.ConsoleTester
{
    class Program
    {
        private static IKernel _kernel;
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

            _workers[0] = _kernel.Get<OpenIncidentWorker>();
            _workers[0].ServiceStarted = true;
            var incst = new ThreadStart(_workers[0].Run);

            _workerThreads[0] = new Thread(incst) { Name = "OpenIncidentWorker" };

            _workers[1] = _kernel.Get<CheckInWorker>();
            _workers[1].ServiceStarted = true;
            var checkst = new ThreadStart(_workers[1].Run);
            _workerThreads[1] = new Thread(checkst) { Name = "CheckInWorker" }; ;

            _workers[2] = _kernel.Get<UpdateTicketClosedWorker>();
            _workers[2].ServiceStarted = true;
            var updateTicketClosedst = new ThreadStart(_workers[2].Run);
            _workerThreads[2] = new Thread(updateTicketClosedst) { Name = "UpdateTicketClosedWorker" }; ;

            var firstWorkerThread = _totalNumberOfThreads - NUMBER_OF_WORKER_THREADS;
            for (var i = firstWorkerThread; i < _totalNumberOfThreads; i++)
            {
                // create an object
                _workers[i] = _kernel.Get<IncidentProcessorWorker>();

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
            _kernel = new StandardKernel();
            _container = new UnityContainer();
            _container.RegisterInstance(_container);

            //Common
            _kernel.Bind<IDeltaLogger>().To<DeltaIncidentProcessorLogger>().InSingletonScope();

            //Repositories
            _kernel.Bind<DbContext>().To<DeltaDbContext>().InThreadScope();

            _kernel.Bind<IRepository>().To<GenericRepository>().InThreadScope();

            _kernel.Bind<IServerRepository>().To<ServerRepository>().InThreadScope();
            _kernel.Bind<IIncidentRepository>().To<IncidentRepository>().InThreadScope();

            //Application Facades
            _kernel.Bind<IIncidentService>().To<IncidentService>().InThreadScope();
            _kernel.Bind<IServerService>().To<ServerService>().InThreadScope();
            _kernel.Bind<IServiceDesk>().To<ServiceDesk>().InThreadScope();

            //Queues
            _kernel.Bind<IQueue<CheckInMessage>>().To<SqlQueue<CheckInMessage>>().WithConstructorArgument("queueTableName", QueueNames.CheckInQueue);
            _kernel.Bind<IQueue<DataCollectionMessage>>().To<SqlQueue<DataCollectionMessage>>().WithConstructorArgument("queueTableName", QueueNames.IncidentProcessorQueue);
            _kernel.Bind<IQueue<DataCollectionArchiveMessage>>().To<SqlQueue<DataCollectionArchiveMessage>>().WithConstructorArgument("queueTableName", QueueNames.DataWarehouseQueue);
            _kernel.Bind<IQueue<DataCollectionMessageWithError>>().To<SqlQueue<DataCollectionMessageWithError>>().WithConstructorArgument("queueTableName", QueueNames.ErrorQueue);
            _kernel.Bind<IQueue<DataCollectionTestMessage>>().To<SqlQueue<DataCollectionTestMessage>>().WithConstructorArgument("queueTableName", QueueNames.TestQueue);
            _kernel.Bind<IQueue<OpenIncidentMessage>>().To<SqlQueue<OpenIncidentMessage>>().WithConstructorArgument("queueTableName", QueueNames.OpenIncidentQueue);
        }
    }
}
