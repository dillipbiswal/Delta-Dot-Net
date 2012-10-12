using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class SqlAgentStatusRule : IncidentProcessorRule
    {
        private string _sqlAgentStatus;
        private string _databaseServerInstanceName;
        private string _sqlInstanceUptime;

        private const string SERVICE_DESK_MATCH_MESSAGE = "The Delta monitoring application has detected the SQL Server Agent is {1} (metricInstanceId: {2}).\n\nDatabase Server Instance Name: {0}\nDatabase Server Instance Status: {1}\n\nAgent Timestamp: {8}\nMetric Threshold: {3}\nMatch Value: {4}\nServer: {5} ({6})\nIp Address: {7}\n";
        private const string SERVICE_DESK_MATCH_COUNT_MESSAGE = "The Delta monitoring application has detected that the SQL Server Agent is {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nDatabase Server Instance Name: {0}\nDatabase Server Instance Status: {1}\n\nAgent Timestamp: {10}\nMetric Threshold: {5}\nMatch Value: {6}\nServer: {7} ({8})\nIp Address: {9}\n";
        private const string SERVICE_DESK_SUMMARY = "P{0}/{1}/SQL Server Agent is {3}";

        public SqlAgentStatusRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Database Status Match";
            XmlMatchString = "SqlAgentStatusPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();
            MatchTypeValue = _sqlAgentStatus;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_MESSAGE, _databaseServerInstanceName, _sqlAgentStatus, MetricInstanceId, _sqlAgentStatus, metricThreshold.Id, metricThreshold.MatchValue, Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_COUNT_MESSAGE, _databaseServerInstanceName, _sqlAgentStatus, MetricInstanceId, count, metricThreshold.TimePeriod, _sqlAgentStatus, metricThreshold.Id, metricThreshold.MatchValue, Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(SERVICE_DESK_SUMMARY, IncidentPriority, Hostname, _databaseServerInstanceName, _sqlAgentStatus);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            var xSqlAgentStatus = dataCollection.Root.Attribute("sqlAgentStatus");
            if (xSqlAgentStatus != null)
            {
                _sqlAgentStatus = xSqlAgentStatus.ToString();
            }

            var xDatabaseServerInstanceName = dataCollection.Root.Attribute("databaseServerInstanceName");
            if (xDatabaseServerInstanceName != null)
            {
                _databaseServerInstanceName = xDatabaseServerInstanceName.Value;
            }

            var XSqlInstanceUptime = dataCollection.Root.Attribute("sqlInstanceUptime");
            if (XSqlInstanceUptime != null)
            {
                _sqlInstanceUptime = XSqlInstanceUptime.Value;
            }

        }
    }
}