using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.MsCluster.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.Plugin.MsCluster
{
    public class MsClusterGroupStatusPlugin : IPlugin
    {
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private readonly IClusterInfo _clusterInfo;
        private readonly IClusterInfrastructure _clusterInfrastructure;

        private Guid _metricInstance;
        private string _label;
        private string _output;

        //Specific
        private string _clusterGroupName;
        private Guid _virtualServerId;

        public MsClusterGroupStatusPlugin()
        {
            _clusterInfo = new ClusterInfo();
            _clusterInfrastructure = new ClusterInfrastructure();
            var common = new Common();
            if (common.GetAgentVersion().Contains("4.0."))
            {
                _dataQueuer = new DataQueuer();
            }
            else
            {
                _dataQueuer = new DotNetDataQueuer();
            }
            _logger = new DeltaLogger();
        }

        public MsClusterGroupStatusPlugin(IClusterInfo clusterInfo, IClusterInfrastructure clusterInfrastructure, IDataQueuer dataQueuer, IDeltaLogger logger)
        {
            _clusterInfo = clusterInfo;
            _clusterInfrastructure = clusterInfrastructure;
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("MsClusterGroupStatus.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                           metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                if (_clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName))
                {
                    GetMsClusterGroupStatus();
                    _dataQueuer.Queue(_output);
                    _logger.LogDebug("Data Queued: " + _output);
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
                try
                {
                    _output = _logger.BuildErrorOutput("MsClusterGroupStatusPlugin", "Execute", _metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }

        }

        private void ParseData(string data)
        {
            var xmlData = XElement.Parse(data);

            Guard.ArgumentNotNull(xmlData.Attribute("ClusterGroupName"), "ClusterGroupName");
            Guard.ArgumentNotNull(xmlData.Attribute("VirtualServerId"), "VirtualServerId");

            // ReSharper disable PossibleNullReferenceException
            _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            _virtualServerId = Guid.Parse(xmlData.Attribute("VirtualServerId").Value);
            // ReSharper restore PossibleNullReferenceException          
        }


        private void GetMsClusterGroupStatus()
        {
            const string resultCode = "0";
            var resultMessage = string.Empty;

            var activeNode = _clusterInfrastructure.GetActiveNodeForGroup(_clusterGroupName);
            var groupStatus = _clusterInfrastructure.GetStatusForGroup(_clusterGroupName);

            BuildExecuteOutput(activeNode, groupStatus, resultCode, resultMessage);
        }


        private void BuildExecuteOutput(string activeNode, string groupStatus, string resultCode, string resultMessage)
        {
            var xml = new XElement("MsClusterGroupStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("virtualServerId", _virtualServerId),
                                   new XAttribute("clusterGroupName", _clusterGroupName),
                                   new XAttribute("activeNode", activeNode),
                                   new XAttribute("status", groupStatus));

            _output = xml.ToString();
        }
    }
}
