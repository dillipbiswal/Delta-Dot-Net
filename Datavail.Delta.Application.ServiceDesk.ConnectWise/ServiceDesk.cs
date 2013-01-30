using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Util;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Services.Protocols;
using System.Xml.Linq;

namespace Datavail.Delta.Application.ServiceDesk.ConnectWise
{
    public class ServiceDesk : IServiceDesk
    {
        private readonly IDeltaLogger _logger;

        public ServiceDesk(IDeltaLogger logger) { _logger = logger; }

        public string OpenIncident(string serviceDeskData)
        {
            Guard.IsNotNull(serviceDeskData, "ServiceDeskData cannot be null");
            var xmlData = XElement.Parse(serviceDeskData);

            Guard.IsNotNull(xmlData.Attribute("IncidentBody"));
            Guard.IsNotNull(xmlData.Attribute("IncidentSummary"));
            Guard.IsNotNull(xmlData.Attribute("IncidentPriority"));

            // ReSharper disable PossibleNullReferenceException
            var config = xmlData.Element("ServiceDeskData").Elements("ServiceDesks").Elements("ServiceDesk").FirstOrDefault(e => e.Attribute("Name").Value == "ConnectWise");

            Guard.IsNotNull(config);
            Guard.IsNotNull(config.Attribute("IncidentCustomer"));
            Guard.IsNotNull(config.Attributes("IntegrationLoginId"));
            Guard.IsNotNull(config.Attributes("IntegrationPassword"));

            var company = config.Attribute("IncidentCustomer").Value;
            var body = xmlData.Attribute("IncidentBody").Value;
            var subject = xmlData.Attribute("IncidentSummary").Value;
            var priority = xmlData.Attribute("IncidentPriority").Value;
            var integrationLogin = config.Attribute("IntegrationLoginId").Value;
            var integrationPassword = config.Attribute("IntegrationPassword").Value;

            var incidentContact = "noone@none.com";
            if (xmlData.Attribute("IncidentContact") != null)
            {
                incidentContact = xmlData.Attribute("IncidentContact").Value;
            }
            // ReSharper restore PossibleNullReferenceException

            var cw = new ConnectWiseWs.integration_io();

            var priorityString = string.Empty;
            switch (priority)
            {
                case "1":
                    priorityString = "Priority 1 - Incident - 30 Minute Response";
                    break;
                case "2":
                    priorityString = "Priority 2 - Incident - 4 Hour Response";
                    break;
                case "3":
                    priorityString = "Priority 3 - Incident - One business Day";
                    break;
                case "4":
                    priorityString = "Priority 3 - Incident - One business Day";
                    break;
            }

            const string pattern = @"\<ProblemDescription\>(.*)\</ProblemDescription\>";
            const string replacement = @"<ProblemDescription>**Redacted**</ProblemDescription>";

            var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"),
                                    new XElement("UpdateTicketAction",
                                                 new XElement("CompanyName", "datavail"),
                                                 new XElement("IntegrationLoginId", integrationLogin),
                                                 new XElement("IntegrationPassword", integrationPassword),
                                                 new XElement("SrServiceRecid", "0"),
                                                 new XElement("Ticket",
                                                              new XElement("Summary", subject),
                                                              new XElement("Status", "N"),
                                                              new XElement("NewTicketContactEmailLookup", incidentContact),
                                                              new XElement("ProblemDescription", body),
                                                              new XElement("Priority", priorityString)),
                                                 new XElement("CompanyId", company)));


            var openText = Regex.Replace(xml.ToString(), pattern, replacement, RegexOptions.Multiline);
            _logger.LogDebug(string.Format("Opening Ticket with XML:\r\n{0}", openText));

            var ticketNumberXml = cw.ProcessClientAction(xml.ToString());
            var doc = XDocument.Parse(ticketNumberXml);

            var receiveText = Regex.Replace(doc.ToString(), pattern, replacement, RegexOptions.Multiline);
            _logger.LogDebug(string.Format("Received XML back from ConnectWise:\r\n{0}", receiveText));

            var firstOrDefault = doc.Descendants("SrServiceRecid").FirstOrDefault();
            if (firstOrDefault != null)
            {
                var ticketNumber = firstOrDefault.Value;
                return ticketNumber;
            }

            return "-1";
        }

        public string UpdateIncident(string serviceDeskData)
        {
            throw new NotImplementedException();
        }

        public string GetIncidentStatus(string serviceDeskData)
        {
            try
            {
                Guard.IsNotNull(serviceDeskData, "ServiceDeskData cannot be null");
                var xmlData = XElement.Parse(serviceDeskData);

                Guard.IsNotNull(xmlData.Attribute("IncidentNumber"));

                // ReSharper disable PossibleNullReferenceException
                var incidentNumber = xmlData.Attribute("IncidentNumber").Value;
                // ReSharper restore PossibleNullReferenceException

                var cw = new ConnectWiseWs.integration_io();

                var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"),
                    new XElement("GetTicketAction",
                        new XElement("CompanyName", "datavail"),
                        new XElement("IntegrationLoginId", "Delta2"),
                        new XElement("IntegrationPassword", "delta"),
                        new XElement("SrServiceRecid", incidentNumber)));

                var ticketXml = cw.ProcessClientAction(xml.ToString());
                var doc = XDocument.Parse(ticketXml);

                var boardName = string.Empty;
                var board = doc.Descendants("Board").FirstOrDefault();
                if(board!=null)
                {
                    boardName = board.Value;
                }

                var firstOrDefault = doc.Descendants("StatusName").FirstOrDefault();
                if (firstOrDefault != null)
                {
                    var status = boardName=="Canceled" ? "Canceled" : firstOrDefault.Value;
                    return status;
                }
                return string.Empty;
            }
            catch (SoapException ex)
            {
                return ex.Message.Contains("ConnectWise.PSA.Common.RecordNotFoundException") ? "Closed" : string.Empty;
            }
        }
    }
}
