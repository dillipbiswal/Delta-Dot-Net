using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Ninject;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace Datavail.Delta.IncidentProcessor
{
    public class CheckInWorker : WorkerBase
    {
        private readonly IKernel _kernel;
        private readonly IQueue<DataCollectionMessage> _incidentQueue;
        private readonly IDeltaLogger _logger;
        private IRepository _repository;
        private DateTime _nextRunTime;

        public CheckInWorker(IKernel kernel, IDeltaLogger logger, IQueue<DataCollectionMessage> incidentQueue, IRepository repository)
        {
            _repository = repository; //Added on Ninject Conversion
            _kernel = kernel;
            _logger = logger;
            _incidentQueue = incidentQueue;
        }

        public override void Run()
        {
            var isCheckInProcessor = false;
            var runInterval = 5;

            bool.TryParse(ConfigurationManager.AppSettings["IsCheckInProcessor"], out isCheckInProcessor);
            int.TryParse(ConfigurationManager.AppSettings["CheckInRunFrequency"], out runInterval);

            while (ServiceStarted && isCheckInProcessor)
            {
                try
                {
                    _nextRunTime = DateTime.UtcNow.AddMinutes(runInterval);

                    //using (var childContainer = _kernel.CreateChildContainer())
                    {
                        //SetupPerLoopChildContainer(childContainer);
                        var dbContext = new DeltaDbContext();
                        _repository = new GenericRepository(dbContext, _logger);

                        var checkInGuid = Guid.Parse("5AC60801-A66A-4967-8BDD-4BC1CFFCC652");
                        var metricInstances = _repository.GetQuery<MetricInstance>().Where(mi => mi.Status == Status.Active && mi.Metric.Id == checkInGuid);

                        foreach (var metricInstance in metricInstances)
                        {
                            var lastCheckInMinutes = (DateTime.UtcNow - metricInstance.Server.LastCheckIn).Minutes;

                            var xml = new XElement("CheckInPluginOutput",
                                                   new XAttribute("timestamp", DateTime.UtcNow),
                                                   new XAttribute("metricInstanceId", metricInstance.Id),
                                                   new XAttribute("label", metricInstance.Label),
                                                   new XAttribute("resultCode", 0),
                                                   new XAttribute("resultMessage", string.Empty),
                                                   new XAttribute("product", Environment.OSVersion.Platform),
                                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                                   new XAttribute("productEdition", string.Empty),
                                                   new XAttribute("minutesSinceLastCheckin", lastCheckInMinutes));

                            var msg = new DataCollectionMessage
                            {
                                Data = xml.ToString(),
                                Hostname = metricInstance.Server.Hostname,
                                IpAddress = metricInstance.Server.IpAddress,
                                ServerId = metricInstance.Server.Id,
                                TenantId = metricInstance.Server.Tenant.Id,
                                Timestamp = DateTime.UtcNow
                            };
                            _incidentQueue.AddMessage(msg);
                        }

                        //Don't run more often than every minute
                        while (DateTime.UtcNow < _nextRunTime)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogUnhandledException("Unhandled Exception", ex);
                }
            }
        }

        private void SetupPerLoopChildContainer(IKernel childKernel)
        {
            childKernel.Bind<DbContext>().To<DeltaDbContext>().InSingletonScope();

            _repository = childKernel.Get<IRepository>();
        }
    }
}