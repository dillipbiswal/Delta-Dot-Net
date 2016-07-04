using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class DiskInfoThresholdRule : IncidentProcessorRule
    {
        private long _totalBytes;
        private string _totalBytesFriendly = string.Empty;
        private long _availableBytes;
        private string _availableBytesFriendly = string.Empty;
        private float _percentageAvailable;

        private const string ServiceDeskMessage = "The Delta monitoring application has detected a disk {0} threshold breach for {1} (metricInstanceId: {2}).\n\nTotal Bytes: {3} ({4:N0})\nAvailable Bytes: {5} ({6:N0})\nPercentage Available: {7:0.00}%\n\nAgent Timestamp (UTC): {14}\nMetric Threshold: {8}\nFloor Value: {9:N2}\nCeiling Value: {10:N2}\nServer: {11} ({12})\nIp Address: {13}\n";
        private const string ServiceDeskMessageCount = "The Delta monitoring application has detected a disk {0} threshold breach for {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nTotal Bytes: {5} ({6:N0})\nAvailable Bytes: {7} ({8:N0})\nPercentage Available: {9:0.00}%\n\nAgent Timestamp (UTC): {16}\nMetric Threshold: {10}\nFloor Value: {11:N2}\nCeiling Value: {12:N2}\nServer: {13} ({14})\nIp Address: {15}\n";
        private const string ServiceDeskMessageAverage = "The Delta monitoring application has detected a disk {0} threshold breach for {1} (metricInstanceId: {2}). The average has been {3:0.00} over the last {4} samples.\n\nTotal Bytes: {5} ({6:N0})\nAvailable Bytes: {7} ({8:N0})\nPercentage Available: {9:0.00}%\n\nAgent Timestamp (UTC): {16}\nMetric Threshold: {10}\nFloor Value: {11:N2}\nCeiling Value: {12:N2}\nServer: {13} ({14})\nIp Address: {15}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Disk {2} threshold breach for {3}";

        public DiskInfoThresholdRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Disk Info Threshold Breach";
            XmlMatchString = "DiskPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            PercentageTypeLabel = "percentage available";
            ValueTypeLabel = "bytes available";
            PercentageTypeValue = _percentageAvailable;
            ValueTypeValue = _availableBytes;
        }


        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, metricTypeDescription, Label);
            return message;
        }

        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, metricTypeDescription, Label, MetricInstanceId, _totalBytesFriendly, _totalBytes, _availableBytesFriendly, _availableBytes, _percentageAvailable, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageCount, metricTypeDescription, Label, MetricInstanceId, count, metricThreshold.TimePeriod, _totalBytesFriendly, _totalBytes, _availableBytesFriendly, _availableBytes, _percentageAvailable, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatAverageServiceDeskMessage(float average, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageAverage, metricTypeDescription, Label, MetricInstanceId, average, metricThreshold.TimePeriod, _totalBytesFriendly, _totalBytes, _availableBytesFriendly, _availableBytes, _percentageAvailable, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            var xTotalBytes = dataCollection.Root.Attribute("totalBytes");
            if (xTotalBytes != null)
            {
                _totalBytes = long.Parse(xTotalBytes.Value);
            }

            var xTotalBytesFriendly = dataCollection.Root.Attribute("totalBytesFriendly");
            if (xTotalBytesFriendly != null)
            {
                _totalBytesFriendly = xTotalBytesFriendly.Value;
            }

            var xAvailableBytes = dataCollection.Root.Attribute("availableBytes");
            if (xAvailableBytes != null)
            {
                _availableBytes = long.Parse(xAvailableBytes.Value);
            }

            var xAvailableBytesFriendly = dataCollection.Root.Attribute("availableBytesFriendly");
            if (xAvailableBytesFriendly != null)
            {
                _availableBytesFriendly = xAvailableBytesFriendly.Value;
            }

            var xPercentage = dataCollection.Root.Attribute("percentageAvailable");
            if (xPercentage != null)
            {
                _percentageAvailable = float.Parse(xPercentage.Value);
            }
        }
    }
}
