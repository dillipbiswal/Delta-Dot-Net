using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class Customer : DomainBase, IServerGroupParent, IMetricConfigurationParent, IMaintenanceWindowParent
    {
        #region Fields
        #endregion

        #region Properties
        
        
        public virtual IList<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual IList<MaintenanceWindow> MaintenanceWindows { get; set; }
        public virtual IList<Cluster> Clusters { get; set; }
        public string Name { get; set; }
        public virtual IList<ServerGroup> ServerGroups { get; set; }
        public virtual IList<Server> Servers { get; set; }
        public string ServiceDeskData { get; set; }
        public virtual StatusWrapper Status { get; set; }
        public virtual Tenant Tenant { get; set; }
        #endregion

        #region ctor
        //[Obsolete("Use static NewCustomer factory instead")]
        public Customer()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static Customer NewCustomer(Tenant tenant, string name)
        {
            var entity = new Customer() { Tenant = tenant, Name = name, Status = Domain.Status.Active };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            MetricConfigurations = new List<MetricConfiguration>();
            MaintenanceWindows = new List<MaintenanceWindow>();
            ServerGroups = new List<ServerGroup>();
        }
        #endregion

        #region Methods
        public override bool CanDelete()
        {
            return MetricConfigurations.Count == 0 && ServerGroups.Count == 0 && Servers.Count == 0;
        }
        #endregion
    }
}
