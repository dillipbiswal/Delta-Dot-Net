using System;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Domain
{
    public class MaintenanceWindow : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public virtual Status ParentPreviousStatus { get; set; }
        #endregion

        #region Parent
        public virtual IMaintenanceWindowParent Parent
        {
            get
            {
                if (Customer != null) return Customer;
                if (Metric != null) return Metric;
                if (MetricInstance != null) return MetricInstance;
                if (Server != null) return Server;
                if (ServerGroup != null) return ServerGroup;
                return Tenant;
            }
            set
            {
                Customer = value as Customer;

                if (Customer == null)
                {
                    Metric = value as Metric;
                }

                if (Metric == null)
                {
                    MetricInstance = value as MetricInstance;
                }

                if (MetricInstance == null)
                {
                    Server = value as Server;
                }

                if (Server == null)
                {
                    ServerGroup = value as ServerGroup;
                }

                if (ServerGroup == null)
                {
                    Tenant = value as Tenant;
                }
            }
        }

        public virtual Customer Customer { get; set; }
        public virtual Metric Metric { get; set; }
        public virtual MetricInstance MetricInstance { get; set; }
        public virtual Server Server { get; set; }
        public virtual ServerGroup ServerGroup { get; set; }
        public virtual Tenant Tenant { get; set; }
        #endregion

        #region ctor
        //[Obsolete("Use static NewMaintenanceWindow factory instead")]
        public MaintenanceWindow()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static MaintenanceWindow NewMaintenanceWindow(IMaintenanceWindowParent parent, DateTime beginDate, DateTime endDate)
        {
            Guard.IsNotNull(parent, "parent");
            Guard.IsTrue(endDate > beginDate, "EndDate must be after BeginDate");
            Guard.IsTrue(beginDate > DateTime.UtcNow, "BeginDate must be in the future");

            var entity = new MaintenanceWindow { Parent = parent, BeginDate = beginDate, EndDate = endDate };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        #endregion
    }
}
