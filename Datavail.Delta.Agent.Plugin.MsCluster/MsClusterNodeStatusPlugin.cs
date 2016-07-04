using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.MsCluster.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;

namespace Datavail.Delta.Agent.Plugin.MsCluster
{
    class MsClusterNodeStatusPlugin
    {

        private readonly IClusterInfo _clusterInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private readonly IDatabaseServerInfo _databaseServerInfo;

        private Guid _metricInstance;
        private string _label;
        private string _output;

        //Specific
        private string _clusterGroupName;
        private bool _runningOnCluster = false;

        public MsClusterNodeStatusPlugin()
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

        public MsClusterNodeStatusPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("MsClusterNodeStatus.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                           metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                if (!_runningOnCluster || (_runningOnCluster && _clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName)))
                {
                    GetMsClusterNodeStatus();
                    _dataQueuer.Queue(_output);
                    _logger.LogDebug("Data Queued: " + _output);
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
                try
                {
                    _output = _logger.BuildErrorOutput("MsClusterNodeStatusPlugin", "Execute", _metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }

        }

        private void ParseData(string data)
        {
            var crypto = new Encryption();
            var xmlData = XElement.Parse(data);


            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }


        private void GetMsClusterNodeStatus()
        {
            var clusterExe = Path.Combine(Environment.SystemDirectory, "cluster.exe");

            var arguments = "NODE";
            var startInfo = new ProcessStartInfo(clusterExe, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(startInfo))
            {
                using (var reader = process.StandardOutput)
                {
                    var output = reader.ReadToEnd();
                    var lines = output.Split(new[] { "\r\n" }, StringSplitOptions.None);

                    var nextLine = false;
                    foreach (var line in lines)
                    {
                        if (nextLine)
                        {
                            var node = line.Substring(0, 20).Trim();
                            var nodeId = line.Substring(21, 15).Trim();
                            var status = line.Substring(36, line.Length - 36).Trim();
                        }

                        if (line.Contains("-------------------"))
                        {
                            nextLine = true;
                        }
                    }
                }
            }
        }


        private void BuildExecuteOutput(string node, string nodeId, string status, string resultCode, string resultMessage)
        {
            var xml = new XElement("MsClusterNodeStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("node", node),
                                   new XAttribute("nodeId", nodeId),
                                   new XAttribute("status", status));


            _output = xml.ToString();
        }


    }
}
