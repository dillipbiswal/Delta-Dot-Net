using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
    public class Constants
    {

        #region "icons/images"

        //TODO make images DIV and move these to CSS
        //Hierarchy Items
        public const string SERVERICON = "Content/images/server.png";
        public const string CLUSTERICON = "Content/images/cluster.png";
        public const string CLUSTERGROUPICON = "Content/images/server-groups.png";
        public const string SERVERGROUPICON = "Content/images/server-groups.png";
        public const string DBINSTANCEICON = "Content/images/cluster.png";
        public const string DBICON = "Content/images/cluster.png";
        public const string CLUSTERNODEICON = "Content/images/server.png";
        public const string METRICICON = "Content/images/metric.png";

        //Actions
        public const string EDITICON = "Content/images/navicons-small/165.png";
        public const string DELETEICON = "Content/images/navicons-small/172.png";
        public const string ADDICON = "Content/images/navicons-small/171.png";

        //Status
        public const string ACTIVEICON = "Content/images/green-indicator.gif";
        public const string INACTIVEICON = "Content/images/red-indicator.gif";
        public const string INMAINTENANCEICON = "Content/images/yellow-indicator.gif";
        public const string UNKNOWN = "";
        public const string DELETED = "";

        public enum SpecificationType
        {
            None = 0,
            CustomerServers = 1,
            UnknownServers = 2,
            ClusterNodeServers = 3,
            ServerGroupServers = 4,
            ServerInstance = 6,
            ClusterServers = 7,
            ServerMetrics = 8,
            DatabaseInstanceMetrics = 9,
            DatabaseMetrics = 10,
            VirtualServerMetrics = 11
        }

        public enum TableType
        {
            None = 0,
            Server = 1,
            ServerGroup = 2,
            Customer = 3,
            Cluster = 4,
            ClusterNode = 6,
            DatabaseInstance = 7,
            Database = 8,
            Metric = 9,
            MetricInstance = 10,
            User = 11,
            MetricThresholds = 12,
            Schedules = 13,
            MetricConfig = 14,
            MaintWindow = 15
        }

        public enum MetricDataRenderType
        {
            Text,
            SelectList,
            MultipleValues
        }

        public enum ItemHierarchyType
        {
            None,
            Tenant,
            Customer,
            Cluster,
            ClusterFolder,
            ServerGroup,
            ServerGroupFolder,
            Server,
            ServerFolder,
            ClusterNode,
            ClusterNodeFolder,
            VirtualServer,
            VirtualServerFolder,
            DatabaseInstance,
            DatabaseInstanceFolder,
            Database,
            DatabaseFolder,
            MetricInstance,
            MetricInstanceFolder
        }

        public enum MetricHierarchyType
        {
            None,
            Metric,
            MetricFolder
        }

        public const int SERVERGROUPFOLDERID = 1;
        public const int CLUSTERFOLDERID = 2;
        public const int SERVERFOLDERID = 3;
        public const int CLUSTERNODEFOLDERID = 4;
        public const int VIRTUALSERVERFOLDERID = 5;
        public const int DATABASEINSTANCEFOLDERID = 6;
        public const int DATABASEFOLDERID = 7;
        public const int METRICINSTANCEFOLDERID = 8;
        public const int SERVERMETRICFOLDERID = 9;
        public const int VIRTUALSERVERMETRICFOLDERID = 10;
        public const int DATABASEINSTANCEMETRICFOLDERID = 11;
        public const int DATABASEMETRICFOLDERID = 12;
        public const int METRICFOLDERID = 13;
        #endregion

        #region "menu"
        public const string CONFIGMENUITEMID = "mainmenu-config";
        public const string ADMINMENUITEMID = "mainmenu-admin";
        #endregion

        #region "Roles"
        public const string DELTAADMIN = "DeltaAdmin";
        public const string DELTAUSER = "DeltaUser";
        public const string DELTREADONLY = "DeltaReadOnly";
        #endregion
    }
}