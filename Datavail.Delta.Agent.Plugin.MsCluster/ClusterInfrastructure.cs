using System;
using System.Management;

namespace Datavail.Delta.Agent.Plugin.MsCluster
{
    public class ClusterInfrastructure : IClusterInfrastructure
    {
        public string GetActiveNodeForGroup(string clusterGroupName)
        {
            const string sServerPath = @"\\localhost\root\mscluster";
            var scope = new ManagementScope(sServerPath);

            scope.Connect();
            
            if (scope.IsConnected)
            {
                var objectQuery = new ObjectQuery("select groupcomponent, partcomponent from mscluster_nodetoactivegroup");
                using (var searcher = new ManagementObjectSearcher(scope, objectQuery))
                {
                    foreach (ManagementObject clusterNode in searcher.Get())
                    {
                        var nodeNameString = clusterNode["GroupComponent"].ToString();
                        var groupNameString = clusterNode["PartComponent"].ToString();

                        var node = nodeNameString.Split('=');
                        node[1] = node[1].Replace("\"", "");

                        var group = groupNameString.Split('=');
                        group[1] = group[1].Replace("\"", "");

                        if (group[1].ToLower() == clusterGroupName.ToLower())
                        {
                            return node[1].ToLower();
                        }
                    }
                }
            }
            return string.Empty;
        }

        public string GetClusterName()
        {
            return GetClusterName(".");
        }

        public string GetClusterName(string hostname)
        {
            try
            {
                var sServerPath = @"\\" + hostname + @"\Root\MSCluster";
                var scope = new ManagementScope(sServerPath);

                scope.Connect();

                if (scope.IsConnected)
                {
                    var objectQuery = new ObjectQuery("SELECT * FROM MSCluster_Cluster");

                    using (var searcher = new ManagementObjectSearcher(scope, objectQuery))
                    {
                        foreach (var cluster in searcher.Get())
                        {
                            var clusterName = cluster["Name"].ToString();
                            return clusterName;
                        }
                        return string.Empty;
                    }
                }

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public string GetStatusForGroup(string clusterGroupName)
        {
            const string S_SERVER_PATH = @"\\localhost\root\mscluster";
            var scope = new ManagementScope(S_SERVER_PATH);

            scope.Connect();

            if (scope.IsConnected)
            {
                var objectQuery = new ObjectQuery(string.Format("select state from MSCluster_ResourceGroup where name='{0}'", clusterGroupName));
                var searcher = new ManagementObjectSearcher(scope, objectQuery);

                foreach (ManagementObject clusterGroup in searcher.Get())
                {
                    var state = clusterGroup["State"].ToString();
                    switch (state)
                    {
                        case "-1":
                            return "Unknown";
                        case "0":
                            return "Online";
                        case "1":
                            return "Offline";
                        case "2":
                            return "Failed";
                        case "3":
                            return "PartialOnline";
                        case "4":
                            return "Pending";
                    }
                }
            }
            return string.Empty;
        }
    }
}
