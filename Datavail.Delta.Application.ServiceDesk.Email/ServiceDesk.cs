using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Util;
using System;
using System.Configuration;
using System.Net.Mail;
using System.Xml.Linq;

namespace Datavail.Delta.Application.ServiceDesk.Email
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

            var body = xmlData.Attribute("IncidentBody").Value;
            var subject = xmlData.Attribute("IncidentSummary").Value;

            var host = ConfigurationManager.AppSettings["MailerHost"];
            var port = 25;
            var from = ConfigurationManager.AppSettings["MailerFrom"];
            var to = ConfigurationManager.AppSettings["MailerTo"];
            Int32.TryParse(ConfigurationManager.AppSettings["MailerPort"], out port);

            var mailer = new SmtpClient(host, port);
            var message = new MailMessage(from, to, subject, body);

            mailer.Send(message);
            return Guid.NewGuid().ToString();
        }

        public string UpdateIncident(string serviceDeskData)
        {
            throw new NotImplementedException();
        }

        public string GetIncidentStatus(string serviceDeskData)
        {
            return "OPEN";
        }
    }
}