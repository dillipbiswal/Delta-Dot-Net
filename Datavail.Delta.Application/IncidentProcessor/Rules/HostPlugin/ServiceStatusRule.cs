using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class ServiceStatus : IncidentProcessorRule
    {
        private string _serviceStatus;
        private string _serviceName;

        private const string ServiceDeskMatchMessage = "The Delta monitoring application has detected that the service {0} is {1} (metricInstanceId: {2}).\n\nAgent Timestamp (UTC): {7}\nMatch Value: {3}\nMetric Threshold: {4}\nServer: {5}\nIp Address: {6}\n";
        private const string ServiceDeskMatchCountMessage = "The Delta monitoring application has detected that the service {0} is {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nAgent Timestamp (UTC): {9}\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Service {2} is {3}";

        public ServiceStatus( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Service Status Match";
            XmlMatchString = "ServiceStatusOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _serviceStatus;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchMessage, _serviceName, _serviceStatus, MetricInstanceId, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress,Timestamp);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchCountMessage, _serviceName, _serviceStatus, MetricInstanceId, count, metricThreshold.TimePeriod, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress,Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, _serviceName, _serviceStatus);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            Guard.IsNotNull(dataCollection.Root.Attribute("serviceName"), "serviceName");
            Guard.IsNotNull(dataCollection.Root.Attribute("status"), "status");
        
            _serviceName = dataCollection.Root.Attribute("serviceName").Value;
            _serviceStatus = dataCollection.Root.Attribute("status").Value;
        }
    }
}