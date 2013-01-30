using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Domain
{
    public class Server : DomainBase, IMetricConfigurationParent, IMaintenanceWindowParent
    {
        #region Fields
        #endregion

        #region Properties
        public virtual Server ActiveNode { get; set; }
        public string AgentVersion { get; set; }
        public virtual Cluster Cluster { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual IList<DatabaseInstance> Instances { get; set; }
        public bool IsVirtual { get; set; }
        public string ClusterGroupName { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public DateTime LastCheckIn { get; set; }
        public virtual IList<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual IList<MaintenanceWindow> MaintenanceWindows { get; set; }
        public virtual IList<MetricInstance> MetricInstances { get; set; }
        public virtual IList<ServerGroup> ServerGroups { get; set; }
        public virtual Status Status { get; set; }
        public virtual Tenant Tenant { get; set; }
        public virtual Cluster VirtualServerParent { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewServer factory instead")]
        public Server()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static Server NewServer(Tenant tenant, Guid serverId, string hostname, string ipaddress)
        {
            var entity = new Server() { Hostname = hostname, Id = serverId, IpAddress = ipaddress, LastCheckIn = DateFunctions.DateTimeDefault, Status = Domain.Status.Unknown, Tenant = tenant };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            MetricConfigurations = new List<MetricConfiguration>();
            MaintenanceWindows = new List<MaintenanceWindow>();
            MetricInstances = new List<MetricInstance>();
            ServerGroups = new List<ServerGroup>();

        }
        #endregion

        #region Methods
        public override bool CanDelete()
        {
            return false;
        }
        #endregion
    }
}
