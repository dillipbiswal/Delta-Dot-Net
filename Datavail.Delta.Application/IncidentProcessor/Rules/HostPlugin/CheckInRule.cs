using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;



namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class CheckInRule : IncidentProcessorRule
    {
        private int _minutesSinceLastCheckIn;

        private const string ServiceDeskMessage = "The Delta monitoring application has detected a Last Check In threshold breach (metricInstanceId: {0}).\n\nLast Checked In {1} Minutes Ago \n\nMetric Threshold: {2}\nFloor Value: {3:N2}\nCeiling Value: {4:N2}\nServer: {5} ({6})\nIp Address: {7}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Last Check In threshold breach";

        public CheckInRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Last Check In Threshold Breach";
            XmlMatchString = "CheckInPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            PercentageTypeLabel = "N/A";
            ValueTypeLabel = "Last Checked In (Minutes)";
            PercentageTypeValue = -1;
            ValueTypeValue = _minutesSinceLastCheckIn;
        }


        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname);
            return message;
        }

        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, MetricInstanceId, _minutesSinceLastCheckIn, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress);
            return message;
        }

        
        protected override void ParseDataCollection(XDocument dataCollection)
        {
            var xMinutesSinceLastCheckIn = dataCollection.Root.Attribute("minutesSinceLastCheckin");
            if (xMinutesSinceLastCheckIn != null)
            {
                _minutesSinceLastCheckIn = int.Parse(xMinutesSinceLastCheckIn.Value);
            }
        }
    }
}
