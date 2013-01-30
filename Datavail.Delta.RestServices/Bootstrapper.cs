using System.Data.Entity;
using System.Web.Http;
using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Datavail.Delta.Repository.Interface;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.RestServices
{
    public static class Bootstrapper
    {
        public static void Initialise()
        {
            var container = BuildUnityContainer();

            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();

            //Infrastructure 
            container.RegisterType<IDeltaLogger, DeltaLogger>();

            //Repositories
            container.RegisterType<DbContext, DeltaDbContext>();
            container.RegisterType<IRepository, GenericRepository>(new InjectionConstructor(typeof(DbContext), typeof(IDeltaLogger)));
            container.RegisterType<IServerRepository, ServerRepository>(new InjectionConstructor(typeof(DbContext), typeof(IDeltaLogger)));

            //Application Facades
            container.RegisterType<IServerService, ServerService>();

            //Queues
            container.RegisterType<IQueue<CheckInMessage>, SqlQueue<CheckInMessage>>(new InjectionConstructor(QueueNames.CheckInQueue));
            container.RegisterType<IQueue<DataCollectionMessage>, SqlQueue<DataCollectionMessage>>(new InjectionConstructor(QueueNames.IncidentProcessorQueue));
            container.RegisterType<IQueue<DataCollectionArchiveMessage>, SqlQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(QueueNames.DataWarehouseQueue));
            container.RegisterType<IQueue<DataCollectionMessageWithError>, SqlQueue<DataCollectionMessageWithError>>(new InjectionConstructor(QueueNames.ErrorQueue));
            container.RegisterType<IQueue<DataCollectionTestMessage>, SqlQueue<DataCollectionTestMessage>>(new InjectionConstructor(QueueNames.TestQueue));        

            return container;
        }
    }
}