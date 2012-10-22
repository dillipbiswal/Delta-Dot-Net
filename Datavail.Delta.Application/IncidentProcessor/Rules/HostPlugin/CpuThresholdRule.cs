using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;



namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class CpuThresholdRule : IncidentProcessorRule
    {
        private float _percentageCpuUsed;

        private const string ServiceDeskMessage = "The Delta monitoring application has detected a CPU threshold breach (metricInstanceId: {0}).\n\nCPU Utilization Percentage: {1:0.00}%\n\nAgent Timestamp (UTC): {8}\nMetric Threshold: {2}\nFloor Value: {3:N2}\nCeiling Value: {4:N2}\nServer: {5} ({6})\nIp Address: {7}\n";
        private const string ServiceDeskMessageCount = "The Delta monitoring application has detected a CPU threshold breach (metricInstanceId: {0}). This has occurred {1} times in the last {2} minutes.\n\nCPU Utilization Percentage: {3:0.00}%\n\nAgent Timestamp (UTC): {10}\nMetric Threshold: {4}\nFloor Value: {5:N2}\nCeiling Value: {6:N2}\nServer: {7}({8})\nIp Address: {9}\n";
        private const string ServiceDeskMessageAverage = "The Delta monitoring application has detected a CPU threshold breach (metricInstanceId: {0}). The average has been {1:0.00} over the last {2} samples.\n\nCPU Utilization Percentage: {3:0.00}%\n\nAgent Timestamp (UTC): {10}\nMetric Threshold: {4}\nFloor Value: {5:N2}\nCeiling Value: {6:N2}\nServer: {7} ({8})\nIp Address: {9}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/CPU threshold breach";

        public CpuThresholdRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "CPU Threshold Breach";
            XmlMatchString = "CpuPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            PercentageTypeLabel = "percentage available";
            ValueTypeLabel = "N/A";
            PercentageTypeValue = _percentageCpuUsed;
            ValueTypeValue = -1;
        }


        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname);
            return message;
        }

        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, MetricInstanceId, _percentageCpuUsed, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageCount, MetricInstanceId, count, metricThreshold.TimePeriod, _percentageCpuUsed, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatAverageServiceDeskMessage(float average, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageAverage, MetricInstanceId, average, metricThreshold.TimePeriod, _percentageCpuUsed, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            var xPercentageCpuUsed = dataCollection.Root.Attribute("percentageCpuUsed");
            if (xPercentageCpuUsed != null)
            {
                _percentageCpuUsed = float.Parse(xPercentageCpuUsed.Value);
            }
        }
    }
}
