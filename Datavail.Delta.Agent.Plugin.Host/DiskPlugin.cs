using System;
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
    public class DiskPlugin : IPlugin
    {
        private readonly IClusterInfo _clusterInfo;
        private readonly ISystemInfo _systemInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;

        private Guid _metricInstance;
        private string _label;
        private string _drive;
        private long _totalBytes;
        private long _availableBytes;

        private string _totalBytesFriendly;
        private string _availableBytesFriendly;
        private double _percentageFree;

        private const double Mb = 1048576;
        private const double Gb = 1073741824;
        private const double Tb = 1099511627776;

        private string _clusterGroupName;
        private bool _runningOnCluster = false;

        private string _output;

        public DiskPlugin()
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
            _systemInfo = new SystemInfo();
        }

        public DiskPlugin(ISystemInfo systemInfo, IDataQueuer dataQueuer, IDeltaLogger logger, IClusterInfo clusterInfo)
        {
            _systemInfo = systemInfo;
            _dataQueuer = dataQueuer;
            _logger = logger;
            _clusterInfo = clusterInfo;
        }

        #region Execute Methods
        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DiskPlugIn.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}", metricInstance, label, data));
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                if (!_runningOnCluster || (_runningOnCluster && _clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName)))
                {
                    GetFreeDiskSpace();
                    BuildExecuteOutput();

                    _dataQueuer.Queue(_output);
                    _logger.LogDebug("Data Queued: " + _output);
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("Unhandled Exception while running DiskPlugin::Execute({0},{1},{2})", metricInstance, label, data), ex);
            }

        }

        private void ParseData(string data)
        {
            var xmlData = XElement.Parse(data);
            Guard.ArgumentNotNullOrEmptyString(xmlData.Attribute("Path").Value, "Path", "A valid path must be specified for DiskPlugin");

            _drive = xmlData.Attribute("Path").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void GetFreeDiskSpace()
        {
            _systemInfo.GetDiskFreeSpace(_drive, out _totalBytes, out _availableBytes);

            _percentageFree = Math.Round((_availableBytes / (double)_totalBytes) * 100, 2);

            //Total Bytes
            if (_totalBytes >= Tb)
            {
                _totalBytesFriendly = Math.Round(_totalBytes / Tb, 2) + " TB";
            }
            else if (_totalBytes >= Gb)
            {
                _totalBytesFriendly = Math.Round(_totalBytes / Gb, 2) + " GB";
            }
            else if (_totalBytes >= Mb)
            {
                _totalBytesFriendly = Math.Round(_totalBytes / Mb, 2) + " MB";
            }

            //Bytes Available
            if (_availableBytes >= Tb)
            {
                _availableBytesFriendly = Math.Round(_availableBytes / Tb, 2) + " TB";
            }
            else if (_totalBytes >= Gb)
            {
                _availableBytesFriendly = Math.Round(_availableBytes / Gb, 2) + " GB";
            }
            else if (_totalBytes >= Mb)
            {
                _availableBytesFriendly = Math.Round(_availableBytes / Mb, 2) + " MB";
            }
        }

        private void BuildExecuteOutput()
        {
            var xml = new XElement("DiskPluginOutput",
                new XAttribute("timestamp", DateTime.UtcNow),
                new XAttribute("metricInstanceId", _metricInstance),
                new XAttribute("label", _label),
                new XAttribute("resultCode", 0),
                new XAttribute("resultMessage", string.Empty),
                new XAttribute("product", Environment.OSVersion.Platform),
                new XAttribute("productVersion", Environment.OSVersion.Version),
                new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                new XAttribute("productEdition", string.Empty),
                new XAttribute("totalBytes", _totalBytes),
                new XAttribute("totalBytesFriendly", _totalBytesFriendly),
                new XAttribute("availableBytes", _availableBytes),
                new XAttribute("availableBytesFriendly", _availableBytesFriendly),
                new XAttribute("percentageAvailable", _percentageFree));

            _output = xml.ToString();
        }
        #endregion
    }
}
