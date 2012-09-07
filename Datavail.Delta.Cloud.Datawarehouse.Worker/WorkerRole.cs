using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Schedules;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.Ef;
using Datavail.Delta.Repository.Ef.Infrastructure;
using Datavail.Delta.Repository.Interface;

using Datavail.Framework.Azure.Configuration;
using Datavail.Framework.Azure.Queue;
using FluentScheduler;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        private IUnityContainer _container;
        private IDeltaLogger _logger;

        public override void Run()
        {
            RoleEnvironment.Changed += (RoleEnvironment_Changed);

            var storage = new ThreadDbContextStorage();
            DbContextInitializer.Instance().InitializeObjectContextOnce(() =>
            {
                DbContextManager.InitStorage(storage);
                DbContextManager.Init("DeltaConnectionString", new[] { Environment.CurrentDirectory + "\\Datavail.Delta.Repository.Ef.dll" });
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

        void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            if (e.Changes.Any(chg => chg is RoleEnvironmentTopologyChange))
            {
                // Perform an action, for example, you can initialize a client, 
                // or you can recycle the role

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

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.



            return base.OnStart();
        }

        public override void OnStop()
        {
            TaskManager.Stop();
            base.OnStop();
        }

        public void RegisterTypes()
        {
            var account = AzureConfiguration.GetStorageAccount("DeltaStorageConnectionString");
            _container.RegisterInstance(account);

            _container.RegisterInstance(_container);

            //Common
            _container.RegisterType<IDeltaLogger, DeltaLogger>();
            

            //Repositories
            _container.RegisterType<DbContext, DbContext>(new PerThreadLifetimeManager(), new InjectionFactory(f => DbContextManager.CurrentFor("DeltaConnectionString")));
            //_container.RegisterType<DbContext, DeltaConnectionString>(new PerThreadLifetimeManager());
            _container.RegisterType<IRepository, GenericRepository>(new InjectionConstructor("DeltaConnectionString", _container.Resolve<IDeltaLogger>()));
            _container.RegisterType<IServerRepository, ServerRepository>(new InjectionConstructor("DeltaConnectionString", _container.Resolve<IDeltaLogger>()));

            //Queues
            var hostname = ConfigurationManager.AppSettings["RabbitMqHost"];

            _container.RegisterType<IQueue<CheckInMessage>, RabbitMqQueue<CheckInMessage>>(new InjectionConstructor(hostname, QueueNames.CheckInQueue));
            _container.RegisterType<IQueue<DataCollectionMessage>, RabbitMqQueue<DataCollectionMessage>>(new InjectionConstructor(hostname, QueueNames.IncidentProcessorQueue));
            _container.RegisterType<IQueue<DataCollectionArchiveMessage>, RabbitMqQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(hostname, QueueNames.DataWarehouseQueue));
            _container.RegisterType<IQueue<DataCollectionMessageWithError>, RabbitMqQueue<DataCollectionMessageWithError>>(new InjectionConstructor(hostname, QueueNames.ErrorQueue));
            _container.RegisterType<IQueue<DataCollectionTestMessage>, RabbitMqQueue<DataCollectionTestMessage>>(new InjectionConstructor(hostname, QueueNames.TestQueue));
        }
    }
}