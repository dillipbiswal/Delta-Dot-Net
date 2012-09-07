using System;
using System.Data.Entity;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Web.Configuration;
using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Datavail.Delta.Repository.Interface;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.Cloud.Ws
{
    public class UnityInstanceProvider : IInstanceProvider
    {
        private readonly Type _serviceType;
        private readonly IUnityContainer _container;
        private readonly IDeltaLogger _logger;

        public UnityInstanceProvider(Type serviceType)
        {
            _logger = new DeltaLogger();

            try
            {
                _serviceType = serviceType;

                _container = new UnityContainer();

                //Infrastructure 
                _container.RegisterType<IDeltaLogger, DeltaLogger>();

                //Repositories
                _container.RegisterType<DbContext, DeltaDbContext>(new WcfServiceInstanceLifeTimeManager());
                _container.RegisterType<IRepository, GenericRepository>(new InjectionConstructor(typeof(DbContext), typeof(IDeltaLogger)));
                _container.RegisterType<IServerRepository, ServerRepository>(new InjectionConstructor(typeof(DbContext), typeof(IDeltaLogger)));

                //Application Facades
                _container.RegisterType<IServerService, ServerService>();

                //Queues
                _container.RegisterType<IQueue<CheckInMessage>, SqlQueue<CheckInMessage>>(new InjectionConstructor(QueueNames.CheckInQueue));
                _container.RegisterType<IQueue<DataCollectionMessage>, SqlQueue<DataCollectionMessage>>(new InjectionConstructor(QueueNames.IncidentProcessorQueue));
                _container.RegisterType<IQueue<DataCollectionArchiveMessage>, SqlQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(QueueNames.DataWarehouseQueue));
                _container.RegisterType<IQueue<DataCollectionMessageWithError>, SqlQueue<DataCollectionMessageWithError>>(new InjectionConstructor(QueueNames.ErrorQueue));
                _container.RegisterType<IQueue<DataCollectionTestMessage>, SqlQueue<DataCollectionTestMessage>>(new InjectionConstructor(QueueNames.TestQueue));
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in Web Services::UnityInstanceProvider::ctor", ex);
            }
        }

        #region IInstanceProvider Members

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            object instance = null;
            try
            {
                instance = _container.Resolve(_serviceType);

            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }
            return instance;
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            object instance = null;
            try
            {
                instance = GetInstance(instanceContext, null);

            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }
            return instance;
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            if (instance is IDisposable)
                ((IDisposable)instance).Dispose();
        }

        #endregion
    }
}
