using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.EfWithMigrations;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.IncidentProcessor
{
    public class UpdateTicketClosedWorker : WorkerBase
    {
        private readonly IDeltaLogger _logger;
        private DateTime _nextRunTime;

        public UpdateTicketClosedWorker(IDeltaLogger logger)
        {
            _logger = logger;
        }

        public override void Run()
        {

            while (ServiceStarted)
            {
                try
                {
                    _nextRunTime = DateTime.UtcNow.AddMinutes(1);

                    using (var context = new DeltaDbContext())
                    {
                        var repository = new IncidentRepository(context, _logger);
                        var openTickets = repository.Find(new Specification<IncidentHistory>(i => i.IncidentNumber != "-1" && i.CloseTimestamp == null)).OrderBy(i => i.IncidentNumber).ToList();
                        var serviceDesk = new ServiceDesk(_logger);

                        foreach (var incidentHistory in openTickets)
                        {
                            var status = serviceDesk.GetIncidentStatus(GetStatusXml(incidentHistory.IncidentNumber));
                            Console.WriteLine(string.Format("Checking Open Ticket {0}. Status is {1}", incidentHistory.IncidentNumber, status));
                            if (status == "Closed" || status == "Canceled" || status == "Resolved")
                            {
                                incidentHistory.CloseTimestamp = DateTime.UtcNow;

                                repository.Update(incidentHistory);
                                repository.UnitOfWork.SaveChanges();
                            }
                        }
                    }

                    //Don't run more often than every minute
                    if (DateTime.UtcNow < _nextRunTime)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogUnhandledException("Unhandled Exception", ex);
                }
            }

            Thread.CurrentThread.Abort();
        }

        private static string GetStatusXml(string incidentNumber)
        {
            var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"), new XElement("GetIncidentStatus", new XAttribute("IncidentNumber", incidentNumber)));
            return xml.ToString();
        }
    }
}