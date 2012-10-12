using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Util;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.MsClusterPlugin
{
    public sealed class ClusterGroupStatusRule : IncidentProcessorRule
    {
        private string _groupName;
        private string _status;

        private const string SERVICE_DESK_MATCH_MESSAGE = "The Delta monitoring application has detected that the cluster group {0} is {1} (metricInstanceId: {2}).\n\nStatus: {1}\n\nAgent Timestamp: {7}\nMatch Value: {3}\nMetric Threshold: {4}\nServer: {5}\nIp Address: {6}\n";
        private const string SERVICE_DESK_MATCH_COUNT_MESSAGE = "The Delta monitoring application has detected that cluster group {0} is {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nStatus: {1}\n\nAgent Timestamp: {9}\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string SERVICE_DESK_SUMMARY = "P{0}/{1}/Cluster Group {2} is {3}";

        public ClusterGroupStatusRule(IIncidentService incidentService, XDocument dataCollection,
                                      IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Cluster Group Status Match";
            XmlMatchString = "MsClusterGroupStatusPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _status;
        }


        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_MESSAGE, _groupName, _status, MetricInstanceId, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_COUNT_MESSAGE, _groupName, _status, MetricInstanceId, count,
                                        metricThreshold.TimePeriod, metricThreshold.MatchValue, metricThreshold.Id,
                                        Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(SERVICE_DESK_SUMMARY, IncidentPriority, Hostname, _groupName, _status);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            // ReSharper disable PossibleNullReferenceException
            Guard.IsNotNull(dataCollection.Root.Attribute("clusterGroupName"), "clusterGroupName");
            Guard.IsNotNull(dataCollection.Root.Attribute("status"), "status");

            _groupName = dataCollection.Root.Attribute("clusterGroupName").Value;
            _status = dataCollection.Root.Attribute("status").Value;
            // ReSharper restore PossibleNullReferenceException
        }
    }
}