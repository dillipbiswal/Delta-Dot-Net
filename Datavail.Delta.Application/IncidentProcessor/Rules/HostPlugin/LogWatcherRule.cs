using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class LogWatcherMatchStatus : IncidentProcessorRule
    {
        private string _fileName;
        private string _matchingLine;
        private string _matchingExpression;

        private const string ServiceDeskMatchMessage = "The Delta monitoring application has detected that the file {0} contains {1} (metricInstanceId: {2}).\n\nAgent Timestamp: {7}\nMatch Value: {3}\nMetric Threshold: {4}\nServer: {5}\nIp Address: {6}\n";
        private const string ServiceDeskMatchCountMessage = "The Delta monitoring application has detected that the file {0} contains {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nAgent Timestamp: {9}\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Log File {2} matched {3}";

        public LogWatcherMatchStatus( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Log Watcher Status Match";
            XmlMatchString = "LogWatcherPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _matchingLine;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchMessage, _fileName, _matchingLine, MetricInstanceId, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress,Timestamp);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            _matchingExpression = metricThreshold.MatchValue;
            var message = string.Format(ServiceDeskMatchCountMessage, _fileName, _matchingLine, MetricInstanceId, count, metricThreshold.TimePeriod, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress,Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, _fileName, _matchingExpression);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            Guard.IsNotNull(dataCollection.Root.Attribute("matchingLine"), "matchingLine");
            
            _matchingLine = dataCollection.Root.Attribute("matchingLine").Value;

            if(dataCollection.Root.Attribute("fileName") != null)
                _fileName = dataCollection.Root.Attribute("fileName").Value;
        }   
    }
}