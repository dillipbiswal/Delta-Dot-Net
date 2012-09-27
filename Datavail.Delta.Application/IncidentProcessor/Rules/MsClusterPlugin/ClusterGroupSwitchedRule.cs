using System;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Util;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.MsClusterPlugin
{
    public sealed class ClusterGroupSwitchedRule : IncidentProcessorRule
    {
        private string _activeNode;
        private Guid _virtualServerId;
        private string _groupName;

        private const string ServiceDeskMessage = "The Delta monitoring application has detected a Cluster Group Failover for '{0}' (metricInstanceId: {1}).\n\nNew Active Node: {2}\nPrevious Node: {3}\n\nServer: {4} ({5})\nIp Address: {6}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Cluster Group Failover for {2}";

        public ClusterGroupSwitchedRule( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Cluster Node Failover";
            XmlMatchString = "MsClusterGroupStatusPluginOutput";

            SetupMatchParams();
        }
        
        public override bool IsMatch()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            foreach (var metricThreshold in Thresholds)
            {
                string previousNode;
                var failoverOccurred = ServerService.VirtualServerActiveNodeFailoverOccurred(ServerId, _virtualServerId, _activeNode, out previousNode);

                IncidentPriority = (int)metricThreshold.Severity;
                IncidentMesage = FormatStandardServiceDeskMessage(_groupName, _activeNode.ToLower(), previousNode.ToLower(), metricThreshold);
                IncidentSummary = FormatSummaryServiceDeskMessage(_groupName);

                if (failoverOccurred)
                {
                    return true;
                }
            }
            return false;
        }

        protected override string FormatSummaryServiceDeskMessage(string clusterGroupName)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, clusterGroupName);
            return message;
        }

        private string FormatStandardServiceDeskMessage(string groupName, string previousNode, string activeNode, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, groupName, MetricInstanceId, activeNode, previousNode, Hostname, ServerId, IpAddress);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            // ReSharper disable PossibleNullReferenceException
            Guard.IsNotNull(dataCollection.Root.Attribute("activeNode"), "activeNode");
            Guard.IsNotNull(dataCollection.Root.Attribute("virtualServerId"), "virtualServerId");
            Guard.IsNotNull(dataCollection.Root.Attribute("clusterGroupName"), "clusterGroupName");
            Guard.IsNotNull(dataCollection.Root.Attribute("status"), "status");

            _activeNode = dataCollection.Root.Attribute("activeNode").Value;
            _virtualServerId = Guid.Parse(dataCollection.Root.Attribute("virtualServerId").Value);
            _groupName = dataCollection.Root.Attribute("clusterGroupName").Value;
            // ReSharper restore PossibleNullReferenceException
        }
    }
}
