using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Domain
{
    public class ServerGroup : DomainBase, IMetricConfigurationParent, IMaintenanceWindowParent
    {
        #region Fields
        #endregion

        #region Properties

        [Required]
        [StringLength(1024)]
        public virtual string Name { get; set; }
        public virtual IList<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual IList<MaintenanceWindow> MaintenanceWindows { get; set; }

        public virtual IServerGroupParent Parent
        {
            get
            {
                if (ParentCustomer != null)
                    return ParentCustomer;

                return ParentTenant;
            }
            set
            {
                ParentCustomer = value as Customer;
                if (ParentCustomer == null)
                {
                    ParentTenant = value as Tenant;
                }
            }
        }

        public virtual Customer ParentCustomer { get; set; }
        public virtual Tenant ParentTenant { get; set; }

        public virtual int Priority { get; set; }
        public virtual IList<Server> Servers { get; set; }
        public virtual Status Status { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewServerGroup factory instead")]
        public ServerGroup()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static ServerGroup NewServerGroup(IServerGroupParent parent, string name, int priority = 9999)
        {
            var entity = new ServerGroup() { Parent = parent, Name = name, Status = Domain.Status.Active, Priority = priority };
            parent.ServerGroups.Add(entity);

            return entity;
        }
#pragma warning restore 612,618

        private void Initialize(){
            Servers = new List<Server>();
            MetricConfigurations = new List<MetricConfiguration>();
            MaintenanceWindows = new List<MaintenanceWindow>();
            Status = Domain.Status.Active;
        }
        #endregion

        #region Methods
        #endregion


    }
}
