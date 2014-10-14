using System.Data.Entity;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.EfWithMigrations;
using System;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Datavail.Delta.Repository.Interface;
using Ninject;
using Ninject.Activation.Blocks;
using Ninject.Extensions.ChildKernel;

namespace Datavail.Delta.IncidentProcessor
{
    public class UpdateTicketClosedWorker : WorkerBase
    {
        private IKernel _kernel;
        private readonly IDeltaLogger _logger;
        private DateTime _nextRunTime;
        private IRepository _repository;
        private IServerService _serverService;
        private IIncidentService _incidentService;

        public UpdateTicketClosedWorker(IDeltaLogger logger, IKernel kernel)
        {
            _kernel = kernel;
            _logger = logger;
        }

        private void SetupChildKernel(IKernel childKernel)
        {
            childKernel.Bind<DbContext>().To<DeltaDbContext>().InSingletonScope();

            _repository = childKernel.Get<IRepository>();
            _serverService = childKernel.Get<IServerService>();
            _incidentService = childKernel.Get<IIncidentService>();
        }

        public override void Run()
        {

            while (ServiceStarted)
            {
                try
                {
                    _nextRunTime = DateTime.UtcNow.AddMinutes(2);

                    var childKernel = new ChildKernel(_kernel);
                    SetupChildKernel(childKernel);

                    using (var block = new ActivationBlock(childKernel))
                    {
                        var repository = childKernel.Get<IIncidentRepository>();
                        var openTickets = repository.GetQuery<IncidentHistory>(i => i.IncidentNumber != "-1" && i.CloseTimestamp == null && i.OpenTimestamp < DateTime.UtcNow.AddMinutes(-5)).OrderBy(i => i.IncidentNumber).ToList();
                        var serviceDesk = childKernel.Get<IServiceDesk>();

                        foreach (var incidentHistory in openTickets)
                        {
                            var status = serviceDesk.GetIncidentStatus(GetStatusXml(incidentHistory.IncidentNumber));
                            //Console.WriteLine("Checking Open Ticket {0}. Status is {1}", incidentHistory.IncidentNumber, status);
                            if (status == "Closed" || status == "Canceled" || status == "Resolved")
                            {
                                incidentHistory.CloseTimestamp = DateTime.UtcNow;

                                repository.Update(incidentHistory);
                                repository.UnitOfWork.SaveChanges();
                            }
                        }
                    }

                    childKernel.Dispose();

                    //Don't run more often than every minute
                    while (DateTime.UtcNow < _nextRunTime)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogUnhandledException("Unhandled Exception", ex);
                }
            }
        }

        private static string GetStatusXml(string incidentNumber)
        {
            var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"), new XElement("GetIncidentStatus", new XAttribute("IncidentNumber", incidentNumber)));
            return xml.ToString();
        }
    }
}