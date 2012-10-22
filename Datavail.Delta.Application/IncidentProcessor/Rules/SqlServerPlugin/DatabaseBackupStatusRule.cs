using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class DatabaseBackupStatusRule : IncidentProcessorRule
    {

        private int _minsSinceLast;
        private string _databaseName;
        private string _instanceName;

        private const string SERVICE_DESK_MESSAGE = "The Delta monitoring application has detected that database {0} has breached the {1} threshold (metricInstanceId: {2}).\n\nInstance Name: {10}\nMinutes Since Last Backup: {3}\n\nAgent Timestamp (UTC): {11}\nMetric Threshold: {4}\nFloor Value: {5:N2}\nCeiling Value: {6:N2}\nServer: {7} ({8})\nIp Address: {9}\n";
        private const string SERVICE_DESK_MESSAGE_COUNT = "The Delta monitoring application has detected that database {0} has breached the {1} threshold (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nInstance Name: {12}\nMinutes Since Last Backup: {5}\n\nAgent Timestamp (UTC): {13}\nMetric Threshold: {6}\nFloor Value: {7:N2}\nCeiling Value: {8:N2}\nServer: {9} ({10})\nIp Address: {11}\n";
        private const string SERVICE_DESK_MESSAGE_AVERAGE = "The Delta monitoring application has detected the database {0} has breached the {1} threshold (metricInstanceId: {2}). The average has been {3:0.00} over the last {4} samples.\n\nAgent Timestamp (UTC): {14}\nTotal Bytes: {5} ({6:N0})\nMinutes Since Last Backup: {7}\nMetric Threshold: {8}\nFloor Value: {9:N2}\nCeiling Value: {10:N2}\nServer: {11} ({12})\nIp Address: {13}\n";
        private const string SERVICE_DESK_SUMMARY = "P{0}/{1}/{2}/Database minutes since last backup threshold breach for DB {3}";

        public DatabaseBackupStatusRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Database Backup Threshold Breach";
            XmlMatchString = "DatabaseBackupStatusPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            ValueTypeLabel = "minutes since last backup";
            ValueTypeValue = _minsSinceLast;
        }


        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(SERVICE_DESK_SUMMARY, IncidentPriority, Hostname, _instanceName, _databaseName);
            return message;
        }

        //"The Delta monitoring application has detected that database {0} has breached the {1} threshold (metricInstanceId: {2}).\n\nInstance Name: {10}\nMinutes Since Last Backup: {3}\n\nMetric Threshold: {4}\nFloor Value: {5:N2}\nCeiling Value: {6:N2}\nServer: {7} ({8})\nIp Address: {9}\n";
        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MESSAGE, _databaseName, metricTypeDescription, MetricInstanceId, _minsSinceLast, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, _instanceName, Timestamp);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MESSAGE_COUNT, _databaseName, metricTypeDescription, MetricInstanceId, count, metricThreshold.TimePeriod, _minsSinceLast, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, _instanceName, Timestamp);
            return message;
        }

        protected override string FormatAverageServiceDeskMessage(float average, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MESSAGE_AVERAGE, metricTypeDescription, Label, MetricInstanceId, average, metricThreshold.TimePeriod, metricThreshold.Id, metricThreshold.FloorValue, metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, Timestamp);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            // ReSharper disable PossibleNullReferenceException
            Guard.IsNotNull(dataCollection.Root.Attribute("minsSinceLast"), "minsSinceLast");
            Guard.IsNotNull(dataCollection.Root.Attribute("name"), "name");
            Guard.IsNotNull(dataCollection.Root.Attribute("instanceName"), "instanceName");
            // ReSharper restore PossibleNullReferenceException

            var xMinsSinceLast = dataCollection.Root.Attribute("minsSinceLast");
            if (xMinsSinceLast != null)
            {
                _minsSinceLast = int.Parse(xMinsSinceLast.Value);
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
        }
    }
}