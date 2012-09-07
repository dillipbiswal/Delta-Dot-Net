using System;
using System.Data.Entity;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Datavail.Delta.Application;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Azure;
using Datavail.Delta.Infrastructure.Azure.Queue;
using Datavail.Delta.Infrastructure.Azure.QueueMessage;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.Ef;
using Datavail.Delta.Repository.Ef.Infrastructure;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.StaticFactory;

namespace Datavail.Delta.WebServices
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

                var account = CloudConfiguration.GetStorageAccount("DeltaStorageConnectionString");
                _container.RegisterInstance(account);

                _container.RegisterType<DbContext, DbContext>(new InjectionFactory(c => DbContextManager.CurrentFor("DeltaConnectionString")));
                
                //Repositories
                _container.RegisterType<IRepository, GenericRepository>();

                //Application Facades
                _container.RegisterType<IServerService, ServerService>();

                //Queues
                _container.RegisterType<IAzureQueue<CheckInMessage>, AzureQueue<CheckInMessage>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.CheckIns));
                _container.RegisterType<IAzureQueue<DataCollectionMessage>, AzureQueue<DataCollectionMessage>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.Collections));
                _container.RegisterType<IAzureQueue<DataCollectionArchiveMessage>, AzureQueue<DataCollectionArchiveMessage>>(new InjectionConstructor(typeof(Microsoft.WindowsAzure.CloudStorageAccount), AzureConstants.Queues.CollectionArchives));

                //WCF Services
                _container.RegisterType<ICheckInService, CheckInService>();
                _container.RegisterType<ICollectionService, CollectionService>();
                _container.RegisterType<IUpdateService, UpdateService>();
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
