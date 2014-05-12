using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Xml.Linq;

namespace Datavail.Delta.Application.ServiceDesk.ServiceNow
{
    public class ServiceDesk : IServiceDesk
    {
        private readonly IDeltaLogger _logger;

        public ServiceDesk(IDeltaLogger logger) { _logger = logger; }

        public string ServiceDeskName
        {
            get { return "servicenow"; }
        }

        public string OpenIncident(string serviceDeskData)
        {
            Guard.IsNotNull(serviceDeskData, "ServiceDeskData cannot be null");
            var xmlData = XElement.Parse(serviceDeskData);

            Guard.IsNotNull(xmlData.Attribute("IncidentBody"));
            Guard.IsNotNull(xmlData.Attribute("IncidentSummary"));
            Guard.IsNotNull(xmlData.Attribute("IncidentPriority"));

            // ReSharper disable PossibleNullReferenceException
            var config = xmlData.Element("ServiceDeskData").Elements("ServiceDesks").Elements("ServiceDesk").FirstOrDefault(e => e.Attribute("Name").Value == "ServiceNow");

            Guard.IsNotNull(config);
            Guard.IsNotNull(config.Attribute("IncidentFromAddress"));
          
            var from = config.Attribute("IncidentFromAddress").Value;
            var tracker = Guid.NewGuid().ToString();
            var body = xmlData.Attribute("IncidentBody").Value + "\n" + "{Delta Tracking Code:" + tracker + "}";
            var subject = xmlData.Attribute("IncidentSummary").Value;
            var priority = xmlData.Attribute("IncidentPriority").Value;

            var host = ConfigurationManager.AppSettings["ServiceNowMailerHost"];
            var port = 25;
            Int32.TryParse(ConfigurationManager.AppSettings["MailerPort"], out port);
            var to = ConfigurationManager.AppSettings["ServiceNowMailerTo"];

            var mailer = new SmtpClient(host, port);
            var message = new MailMessage(from, to, subject, body);

            mailer.Send(message);
            return tracker;
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

                var json = string.Empty;
                using (var handler = new HttpClientHandler())
                {
                    handler.Credentials = new System.Net.NetworkCredential("datawarehouse", "cw2xHFfS");
                    using (var client = new HttpClient(handler))
                    {
                        json =
                            client.GetStringAsync(
                                string.Format(
                                    "https://datavail.service-now.com/incident_list.do?JSON&sysparm_query=u_3rd_party_reference%3D{0}",
                                    incidentNumber)).Result;
                    }
                }
                var tickets = JObject.Parse(json);

                if (!tickets["records"].Any())
                {
                    return "Not Found";
                }
                else
                {
                    var state = (string) tickets["records"][0]["state"];
                    return state;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public bool IsClosedStatus(string status)
        {
            return status == "6" || status == "7" || status == "8" || status == "Not Found";
        }
    }
}