using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class Tenant : DomainBase, IServerGroupParent, IMetricConfigurationParent, IMaintenanceWindowParent
    {
        #region Fields
        #endregion

        #region Properties
        public virtual ICollection<Customer> Customers { get; set; }
        public virtual IList<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual IList<MaintenanceWindow> MaintenanceWindows { get; set; }
        public string Name { get; set; }
        public virtual IList<ServerGroup> ServerGroups { get; set; }
        public virtual IList<Server> Servers { get; set; }
        public virtual Status Status { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewTenant factory instead")]
        public Tenant()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static Tenant NewTenant(string name)
        {
            var entity = new Tenant() { Name = name, Status = Domain.Status.Active };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            Customers = new List<Customer>();
            MetricConfigurations = new List<MetricConfiguration>();
            MaintenanceWindows = new List<MaintenanceWindow>();
            Servers = new List<Server>();
            ServerGroups = new List<ServerGroup>();
        }
        #endregion

        #region Methods
        public override bool CanDelete()
        {
            return Customers.Count == 0 && MetricConfigurations.Count == 0 && ServerGroups.Count == 0 && Servers.Count == 0;
        }
        #endregion
    }
}
