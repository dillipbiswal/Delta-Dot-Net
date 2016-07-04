using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.Host.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using System.Management;

namespace Datavail.Delta.Agent.Plugin.Host
{
    public class DiskInventoryPlugin : IPlugin
    {
        private readonly IClusterInfo _clusterInfo;
        private readonly ISystemInfo _systemInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;

        private Guid _metricInstance;
        private string _label;

        private string _output;

        public DiskInventoryPlugin()
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

        public DiskInventoryPlugin(IClusterInfo clusterInfo, ISystemInfo systemInfo, IDataQueuer dataQueuer, IDeltaLogger logger)
        {
            _clusterInfo = clusterInfo;
            _systemInfo = systemInfo;
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DiskPlugIn.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                           metricInstance, label, data));
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                BuildExecuteOutput();
                _dataQueuer.Queue(_output);
            }
            catch (Exception ex)
            {

                _logger.LogUnhandledException("Unhandled Exception", ex);
                try
                {
                    _output = _logger.BuildErrorOutput("DiskInventoryPlugin", "Execute", _metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }
        }

        private void BuildExecuteOutput()
        {
            //var drives = _systemInfo.GetLogicalDrives();

            var xml = new XElement("DiskInventoryPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty));

            foreach (var clusterDisk in _clusterInfo.GetClusterDisks())
            {
                xml.Add(clusterDisk);
            }

            const string queryString = "SELECT * FROM Win32_Volume";
            var query = new SelectQuery(queryString);
            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var disk in searcher.Get())
                {
                    string driveFormat;
                    string driveLabel;
                    long totalSize;
                    //_systemInfo.GetDriveInfo(disk["Name"].ToString(), out driveType, out driveFormat, out totalSize, out driveLabel);


                    if (disk["DriveType"].ToString() == "3" && (!disk["Name"].ToString().ToLower().Contains("volume")) &&
                        xml.Descendants().Where(e => e.Name == "Disk").Attributes("path").Where(v => v.Value == disk["Name"].ToString()).Count
                            () == 0)
                    {
                        try
                        {
                            driveFormat = disk["FileSystem"].ToString();
                        }
                        catch (Exception)
                        {
                            driveFormat = "Unknown";
                        }
                        try
                        {
                            if (disk["Label"] == null)
                            {
                                driveLabel = "";
                            }
                            else
                            {
                                driveLabel = disk["Label"].ToString();
                            }
                        }
                        catch (Exception)
                        {
                            driveLabel = "";
                        }
                        try
                        {
                            totalSize = long.Parse(disk.Properties["Capacity"].Value.ToString());
                        }
                        catch (Exception)
                        {
                            totalSize = -1;
                        }
                        var node = new XElement("Disk",
                                                new XAttribute("driveFormat", driveFormat),
                                                new XAttribute("isClusterDisk", "false"),
                                                new XAttribute("label", driveLabel),
                                                new XAttribute("path", disk["Name"].ToString()),
                                                new XAttribute("totalBytes", totalSize));

                        xml.Add(node);
                    }

                }
            }

            //foreach (var drive in drives)
            //{
            //    DriveType driveType;
            //    string driveFormat;
            //    string driveLabel;
            //    long totalSize;

            //    _systemInfo.GetDriveInfo(drive, out driveType, out driveFormat, out totalSize, out driveLabel);


            //    if (driveType == DriveType.Fixed &&
            //        xml.Descendants().Where(e => e.Name == "Disk").Attributes("path").Where(v => v.Value == drive).Count
            //            () == 0)
            //    {
            //        var node = new XElement("Disk",
            //                                new XAttribute("driveFormat", driveFormat),
            //                                new XAttribute("isClusterDisk", "false"),
            //                                new XAttribute("label", driveLabel),
            //                                new XAttribute("path", drive),
            //                                new XAttribute("totalBytes", totalSize));

            //        xml.Add(node);
            //    }
            //}

            _output = xml.ToString();
        }
    }
}