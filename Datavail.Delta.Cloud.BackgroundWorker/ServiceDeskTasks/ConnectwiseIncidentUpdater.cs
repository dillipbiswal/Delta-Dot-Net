using System;
using System.Linq;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Specification;

namespace Datavail.Delta.Cloud.BackgroundWorker.ServiceDeskTasks
{
    public class ConnectwiseIncidentUpdater
    {
        private readonly IRepository _repository;
        private readonly IServiceDesk _serviceDesk;
        private readonly IDeltaLogger _logger;

        public ConnectwiseIncidentUpdater(IRepository repository, IServiceDesk serviceDesk, IDeltaLogger logger)
        {
            _repository = repository;
            _serviceDesk = serviceDesk;
            _logger = logger;
        }

        public void Execute()
        {
            try
            {
                var openTickets = _repository.Find(new Specification<IncidentHistory>(i => i.IncidentNumber != "-1" && i.CloseTimestamp == null)).ToList();

                foreach (var incidentHistory in from incidentHistory in openTickets let status = _serviceDesk.GetIncidentStatus(GetStatusXml(incidentHistory.IncidentNumber)) where status == "Closed" || status == "Canceled" || status == "Resolved" select incidentHistory)
                {
                    incidentHistory.CloseTimestamp = DateTime.UtcNow;

                    _repository.Update(incidentHistory);
                    _repository.UnitOfWork.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Exception in ConnectwiseIncidentUpdater::Execute", ex);
            }

        }

        private static string GetStatusXml(string incidentNumber)
        {
            var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"),
                                    new XElement("GetIncidentStatus", new XAttribute("IncidentNumber", incidentNumber)));

            return xml.ToString();
        }
    }
}
