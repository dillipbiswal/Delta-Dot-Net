using System;
using System.Configuration;
using System.Data.Entity;
using System.Reflection;
using System.Threading;
using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.IncidentProcessor.Tasks.Schedules;
using Datavail.Delta.IncidentProcessor.Tasks.ServiceDeskTasks;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.Ef;
using Datavail.Delta.Repository.Ef.Infrastructure;
using Datavail.Delta.Repository.Interface;
using FluentScheduler;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.IncidentProcessor
{
    public class BackgroundWorker : WorkerBase
    {
        private IUnityContainer _container;
        private IDeltaLogger _logger;
        
        public override void Run()
        {
            while (ServiceStarted)
            {
                var storage = new ThreadDbContextStorage();
                DbContextInitializer.Instance().InitializeObjectContextOnce(() =>
                {
                    DbContextManager.InitStorage(storage);
                    DbContextManager.Init("DeltaConnectionString", new[] { Assembly.GetExecutingAssembly().Location.Replace("Datavail.Delta.IncidentProcessor.exe", "") + "Datavail.Delta.Repository.Ef.dll" });
                });

                _container = new UnityContainer();
                RegisterTypes();

                _logger = _container.Resolve<IDeltaLogger>();

                CreateSchedules();

                while (true)
                {
                    Thread.Sleep(10000);
                }
            }
        }

        private void CreateSchedules()
        {
            try
            {
                TaskManager.Initialize(new ScheduleRegistry(_container, _logger));
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in CreateSchedules()", ex);
            }
        }

        private void RegisterTypes()
        {

            _container.RegisterInstance(_container);

            //Common
            _container.RegisterType<IDeltaLogger, DeltaLogger>();


            //Repositories
            _container.RegisterType<DbContext, DbContext>(new PerThreadLifetimeManager(), new InjectionFactory(f => DbContextManager.CurrentFor("DeltaConnectionString")));
            _container.RegisterType<IRepository, GenericRepository>(new InjectionConstructor("DeltaConnectionString", _container.Resolve<IDeltaLogger>()));
            _container.RegisterType<IServerRepository, ServerRepository>(new InjectionConstructor("DeltaConnectionString", _container.Resolve<IDeltaLogger>()));

            //Application Facades
            _container.RegisterType<IIncidentService, IncidentService>();
            _container.RegisterType<IServerService, ServerService>();
            _container.RegisterType<IServiceDesk, ServiceDesk>();

            //Queues
            var hostname = ConfigurationManager.AppSettings["RabbitMqHost"];

            _container.RegisterType<IQueue<CheckInMessage>, RabbitMqQueue<CheckInMessage>>(new InjectionConstructor(typeof(IDeltaLogger), hostname, QueueNames.CheckInQueue, 5));
            _container.RegisterType<IQueue<DataCollectionMessage>, RabbitMqQueue<DataCollectionMessage>>(new InjectionConstructor(typeof(IDeltaLogger), hostname, QueueNames.IncidentProcessorQueue, 5));
            _container.RegisterType<IQueue<DataCollectionArchiveMessage>, RabbitMqQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(typeof(IDeltaLogger), hostname, QueueNames.DataWarehouseQueue, 5));
            _container.RegisterType<IQueue<DataCollectionMessageWithError>, RabbitMqQueue<DataCollectionMessageWithError>>(new InjectionConstructor(typeof(IDeltaLogger), hostname, QueueNames.ErrorQueue, 5));
            _container.RegisterType<IQueue<DataCollectionTestMessage>, RabbitMqQueue<DataCollectionTestMessage>>(new InjectionConstructor(typeof(IDeltaLogger), hostname, QueueNames.TestQueue, 5));
            //Workers
            _container.RegisterType<ConnectwiseIncidentUpdater, ConnectwiseIncidentUpdater>();
        }
    }
}
