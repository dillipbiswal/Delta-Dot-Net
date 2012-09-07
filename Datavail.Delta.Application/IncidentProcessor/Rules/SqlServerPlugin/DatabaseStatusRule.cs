using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class DatabaseStatusRule : IncidentProcessorRule
    {
        private string _databaseStatus;
        private string _databaseName;
        private string _instanceName;
        private string _databaseId;

        private const string ServiceDeskMatchMessage = "The Delta monitoring application has detected that the database {0} is {1} (metricInstanceId: {2}).\n\nInstance Name: {8}\nDatabase Name: {0}\nDatabase Status: {1}\n\nMatch Value: {3}\nMetric Threshold: {4}\nServer: {6}\nIp Address: {7}\n";
        private const string ServiceDeskMatchCountMessage = "The Delta monitoring application has detected that the database {0} is {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nInstance Name: {10}\nDatabase Name: {0}\nDatabase Status: {1}\n\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {8}\nIp Address: {9}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Database {2} is {3}";

        public DatabaseStatusRule( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Database Status Match";
            XmlMatchString = "DatabaseStatusPluginOutput";
   
            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _databaseStatus;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchMessage, _databaseName, _databaseStatus, MetricInstanceId, _databaseStatus, metricThreshold.Id, metricThreshold.MatchValue, Hostname, IpAddress, _instanceName);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchCountMessage, _databaseName, _databaseStatus, MetricInstanceId, count, metricThreshold.TimePeriod, _databaseStatus, metricThreshold.Id, metricThreshold.MatchValue, Hostname, IpAddress, _instanceName);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, _instanceName, _databaseName, _databaseStatus);
            return message;
        }
        
        protected override void ParseDataCollection(XDocument dataCollection)
        {
            var xDatabaseStatus = dataCollection.Root.Attribute("status");
            if (xDatabaseStatus != null)
            {
                _databaseStatus = xDatabaseStatus.Value;
            }

            var xDatabaseName = dataCollection.Root.Attribute("name");
            if (xDatabaseName != null)
            {
                _databaseName = xDatabaseName.Value;
            }

            var xInstanceName = dataCollection.Root.Attribute("instanceName");
            if (xInstanceName != null)
            {
                _instanceName = xInstanceName.Value;
            }

            var xDatabaseId = dataCollection.Root.Attribute("databaseId");
            if (xDatabaseId != null)
            {
                _databaseId = xDatabaseId.Value;
            }
        }
    }
}