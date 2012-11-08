using System;
using System.Collections.Generic;
using System.Management;
using System.Xml.Linq;
using Datavail.Delta.Infrastructure.Agent.Cluster;

namespace Datavail.Delta.Agent.Plugin.Host.Cluster
{
    public class ClusterInfo : IClusterInfo
    {
        public string NodeStatus { get; set; }
        public string GroupStatus { get; set; }

        public bool IsActiveClusterNodeForGroup(string clusterGroupName)
        {
            const string sServerPath = @"\\localhost\root\mscluster";
            var scope = new ManagementScope(sServerPath);
            var activeNode = false;

            scope.Connect();

            if (scope.IsConnected)
            {
                var objectQuery =
                    new ObjectQuery("select groupcomponent, partcomponent from mscluster_nodetoactivegroup");


                var searcher = new ManagementObjectSearcher(scope, objectQuery);

                foreach (ManagementObject clusterNode in searcher.Get())
                {
                    var nodeNameString = clusterNode["GroupComponent"].ToString();
                    var groupNameString = clusterNode["PartComponent"].ToString();


                    var node = nodeNameString.Split('=');
                    node[1] = node[1].Replace("\"", "");

                    var group = groupNameString.Split('=');
                    group[1] = group[1].Replace("\"", "");

                    if (group[1].ToLower() == clusterGroupName.ToLower() &&
                        Environment.MachineName.ToLower() == node[1].ToLower())
                    {
                        activeNode = true;
                    }
                }
            }
            return activeNode;
        }

        public IEnumerable<XElement> GetClusterDisks()
        {
            try
            {
                var elements = new List<XElement>();

                const string sServerPath = @"\\.\root\mscluster";
                var scope = new ManagementScope();

                try
                {
                    scope = new ManagementScope(sServerPath);
                    scope.Connect();
                }
                catch (Exception)
                {
                }

                if (scope.IsConnected)
                {
                    var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_ResourceToDisk");
                    var searcher = new ManagementObjectSearcher(scope, objectQuery);

                    foreach (var resToDisk in searcher.Get())
                    {
                        var resourceGroupName = GetResourceGroupNameFromResourceName(scope, resToDisk.Properties["GroupComponent"].Value.ToString());
                        var diskPartitions = GetDiskPartitionsInfoFromDisk(scope, resToDisk.Properties["PartComponent"].Value.ToString(), resourceGroupName);
                        elements.AddRange(diskPartitions);
                    }
                }
                return elements;
            }
            catch (Exception)
            {
                return new List<XElement>();
            }

        }

        #region GetClusterDisks Helpers
        private IEnumerable<XElement> GetDiskPartitionsInfoFromDisk(ManagementScope scope, string value, string resourceGroupName)
        {
            var elements = new List<XElement>();
            var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_DiskToDiskPartition WHERE GroupComponent = '" + value + "'");
            var searcher = new ManagementObjectSearcher(scope, objectQuery);

            foreach (var diskToDiskPartition in searcher.Get())
            {
                var inputPath = diskToDiskPartition.Properties["PartComponent"].Value.ToString().Replace("\"", "").Replace("MSCluster_DiskPartition.Path=", "");

                string path, fileSystem, label;
                double totalSize;

                GetDiskInfo(scope, inputPath, out path, out fileSystem, out label, out totalSize);
                var node = new XElement("Disk",
                                        new XAttribute("clusterName", GetClusterName()),
                                        new XAttribute("resourceGroupName", resourceGroupName),
                                        new XAttribute("driveFormat", fileSystem),
                                        new XAttribute("isClusterDisk", "true"),
                                        new XAttribute("label", label),
                                        new XAttribute("path", path),
                                        new XAttribute("totalBytes", totalSize));

                elements.Add(node);
            }

            return elements;
        }

        private static void GetDiskInfo(ManagementScope scope, string value, out string path, out string fileSystem, out string label, out double totalBytes)
        {
            var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_DiskPartition WHERE Path = '" + value + "'");
            var searcher = new ManagementObjectSearcher(scope, objectQuery);

            foreach (var disk in searcher.Get())
            {
                path = value + "\\";
                fileSystem = disk.Properties["FileSystem"].Value.ToString();
                label = disk.Properties["VolumeLabel"].Value.ToString();
                totalBytes = double.Parse(disk.Properties["TotalSize"].Value.ToString()) * 1048576;

                return;
            }

            path = string.Empty;
            fileSystem = string.Empty;
            label = string.Empty;
            totalBytes = default(double);
        }

        private static string GetResourceGroupNameFromResourceName(ManagementScope scope, string value)
        {
            var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_ResourceGroupToResource WHERE PartComponent = '" + value + "'");
            var searcher = new ManagementObjectSearcher(scope, objectQuery);

            foreach (var resGroupToResource in searcher.Get())
            {
                return resGroupToResource.Properties["GroupComponent"].Value.ToString().Replace("\"", "").Replace("MSCluster_ResourceGroup.Name=", "");
            }

            return string.Empty;
        }
        #endregion

        private static string GetClusterName()
        {
            return GetClusterName(".");
        }

        private static string GetClusterName(string hostname)
        {
            try
            {
                var sServerPath = @"\\" + hostname + @"\Root\MSCluster";
                var scope = new ManagementScope(sServerPath);

                scope.Connect();

                if (scope.IsConnected)
                {
                    var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_Cluster");

                    var searcher = new ManagementObjectSearcher(scope, objectQuery);

                    foreach (var cluster in searcher.Get())
                    {
                        var clusterName = cluster["Name"].ToString();
                        return clusterName;
                    }
                    return string.Empty;
                }

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}