using System;
using System.Configuration;
using System.Data.Entity;
using System.Reflection;
using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.IncidentProcessor;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Datavail.Delta.Repository.Interface;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.IncidentProcessorConsole
{
    class Program
    {
        private static IUnityContainer _container;

        static void Main(string[] args)
        {
            //var storage = new ThreadDbContextStorage();
            //DbContextInitializer.Instance().InitializeObjectContextOnce(() =>
            //{
            //    DbContextManager.InitStorage(storage);
            //    DbContextManager.Init("DeltaConnectionString", new[] { Assembly.GetExecutingAssembly().Location.Replace("Datavail.Delta.IncidentProcessorConsole.exe", "") + "Datavail.Delta.Repository.Ef.dll" });
            //});

            RegisterTypes();
            
            var worker = _container.Resolve<IncidentProcessorWorker>();
            worker.ServiceStarted = true;
            worker.Run();
        }

        private static void RegisterTypes()
        {
            _container = new UnityContainer();
            _container.RegisterInstance(_container);

            //Common
            _container.RegisterType<IDeltaLogger, DeltaLogger>();

            //Repositories
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

        }
    }
}
