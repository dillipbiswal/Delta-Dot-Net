using System;
using System.Management;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.Host.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.Plugin.Host
{
    class ServiceStatusPlugin : IPlugin
    {
        private readonly IClusterInfo _clusterInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;

        private Guid _metricInstance;
        private string _label;
        private string _serviceName;
        private string _serviceStatus;

        private string _clusterGroupName;
        private bool _runningOnCluster = false;

        private string _output;

        public ServiceStatusPlugin()
        {
            _clusterInfo = new ClusterInfo();
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

        public ServiceStatusPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, IClusterInfo cluster)
        {
            _clusterInfo = cluster;
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        #region Execute Methods
        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("ServiceStatusPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}", metricInstance, label, data));
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                if (!_runningOnCluster || (_runningOnCluster && _clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName)))
                {
                    GetServiceStatus();
                    BuildExecuteOutput();

                    _dataQueuer.Queue(_output);
                    _logger.LogDebug("Data Queued: " + _output);
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("Unhandled Exception while running ServiceStatusPlugin::Execute({0},{1},{2})", metricInstance, label, data), ex);
            }
        }

        private void GetServiceStatus()
        {
            const string sServerPath = @"\\.\root\cimV2";
            var scope = new ManagementScope(sServerPath);

            scope.Connect();

            if (scope.IsConnected)
            {
                var objectQuery = new ObjectQuery("select * from Win32_Service where name='" + _serviceName + "'");
                var searcher = new ManagementObjectSearcher(scope, objectQuery);

                var found = false;
                foreach (ManagementObject service in searcher.Get())
                {
                    _serviceStatus = service["State"].ToString();
                    found = true;
                }

                if (!found)
                {
                    throw new ArgumentException(string.Format("The service '{0}' does not exist", _serviceName));
                }
            }
        }

        private void ParseData(string data)
        {
            var xmlData = XElement.Parse(data);
            Guard.ArgumentNotNullOrEmptyString(xmlData.Attribute("ServiceName").Value, "ServiceName", "A valid service name must be specified for ServiceStatusPlugin");

            _serviceName = xmlData.Attribute("ServiceName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void BuildExecuteOutput()
        {
            var xml = new XElement("ServiceStatusOutput",
                new XAttribute("timestamp", DateTime.UtcNow),
                new XAttribute("metricInstanceId", _metricInstance),
                new XAttribute("label", _label),
                new XAttribute("resultCode", 0),
                new XAttribute("resultMessage", string.Empty),
                new XAttribute("product", Environment.OSVersion.Platform),
                new XAttribute("productVersion", Environment.OSVersion.Version),
                new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                new XAttribute("productEdition", string.Empty),
                new XAttribute("serviceName", _serviceName),
                new XAttribute("status", _serviceStatus));

            _output = xml.ToString();
        }
        #endregion
    }
}