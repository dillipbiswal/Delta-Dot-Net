using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class PerfMetricsLazyWritesPerSecRule : IncidentProcessorRule
    {
        private long _lazyWritesPerSec;
        private string _instanceName;

        private const string ServiceDeskMessage = "The Delta monitoring application has detected a performance metrics threshold breach for {0} (metricInstanceId: {1}).\n\nInstance Name: {9}\nLazy Writes per Second: {2} \n\nMetric Threshold: {3}\nFloor Value: {4}\nCeiling Value: {5}\nServer: {6} ({7})\nIp Address: {8}\n";
        private const string ServiceDeskMessageCount = "The Delta monitoring application has detected a performance metrics threshold breach for {0} (metricInstanceId: {1}).This has occurred {2} times in the last {3} minutes.\n\nInstance Name: {11}\nLazy Writes per Second: {4} \n\nMetric Threshold: {5}\nFloor Value: {6}\nCeiling Value: {7}\nServer: {8} ({9})\nIp Address: {10}\n";
        private const string ServiceDeskMessageAverage = "The Delta monitoring application has detected a performance metrics threshold breach for  {0} (metricInstanceId: {2}). The average has been {3:0.00} over the last {4} samples.\n\nLazy Writes per Second: {5} \n\nInstance Name: {12}\nMetric Threshold: {6}\nFloor Value: {7}\nCeiling Value: {8}\nServer: {9} ({10})\nIp Address: {11}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Performance Metrics ({2}) threshold breach";

        public PerfMetricsLazyWritesPerSecRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Performance Metric (Lazy Writes per Second Threshold Breach)";
            XmlMatchString = "DatabaseServerPerformanceCountersPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = "Lazy Writes/Sec";
            PercentageTypeValue = 0;
            ValueTypeValue = 100000;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, MatchTypeValue);
            return message;
        }

        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, MatchTypeValue, MetricInstanceId, _lazyWritesPerSec, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, _instanceName);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageCount, MatchTypeValue, MetricInstanceId, metricThreshold.NumberOfOccurrences, metricThreshold.TimePeriod, _lazyWritesPerSec, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, _instanceName);
            return message;
        }

        protected override string FormatAverageServiceDeskMessage(float average, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageAverage, MatchTypeValue, Label, MetricInstanceId, average, metricThreshold.TimePeriod, _lazyWritesPerSec, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, _instanceName);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            var XLazyWritesPerSec = dataCollection.Root.Attribute("lazyWritesPerSec");
            if (XLazyWritesPerSec != null)
            {
                _lazyWritesPerSec = long.Parse(XLazyWritesPerSec.Value);
            }

            var xInstanceName = dataCollection.Root.Attribute("instanceName");
            if (xInstanceName != null)
            {
                _instanceName = xInstanceName.Value;
            }
        }
    }
}
