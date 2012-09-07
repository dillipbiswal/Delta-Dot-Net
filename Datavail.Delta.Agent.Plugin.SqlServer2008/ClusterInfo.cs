using System;
using System.Collections.Generic;
using System.Management;
using System.Xml.Linq;
using Datavail.Delta.Infrastructure.Agent.Cluster;

namespace Datavail.Delta.Agent.Plugin.SqlServer2008.Infrastructure
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
                    var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_DiskPartition");
                    var searcher = new ManagementObjectSearcher(scope, objectQuery);

                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var node = new XElement("Disk",
                                                new XAttribute("clusterName", GetClusterName()),
                                                new XAttribute("driveFormat", disk.Properties["FileSystem"].Value),
                                                new XAttribute("isClusterDisk", "true"),
                                                new XAttribute("label", disk.Properties["VolumeLabel"].Value),
                                                new XAttribute("path", disk.Properties["Path"].Value + "\\"),
                                                new XAttribute("totalBytes",
                                                               double.Parse(disk.Properties["TotalSize"].Value.ToString()) *
                                                               1048576));
                        elements.Add(node);
                    }
                }
                return elements;
            }
            catch (Exception)
            {
                return new List<XElement>();
            }

        }

        private string GetClusterName()
        {
            return GetClusterName(".");
        }

        private string GetClusterName(string hostname)
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