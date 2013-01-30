using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class MetricConfiguration : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public virtual Metric Metric { get; set; }
        public string Name { get; set; }

        #region Parent
        public virtual IMetricConfigurationParent Parent
        {
            get
            {
                if (ParentCustomer != null) return ParentCustomer;
                if (ParentMetric != null) return ParentMetric;
                if (ParentMetricInstance != null) return ParentMetricInstance;
                if (ParentServer != null) return ParentServer;
                if (ParentServerGroup != null) return ParentServerGroup;
                return ParentTenant;
            }
            set
            {
                ParentCustomer = value as Customer;

                if (ParentCustomer == null)
                {
                    ParentMetric = value as Metric;
                }

                if (ParentMetric == null)
                {
                    ParentMetricInstance = value as MetricInstance;
                }

                if(ParentMetricInstance == null)
                {
                    ParentServer = value as Server;
                }

                if(ParentServer == null)
                {
                    ParentServerGroup = value as ServerGroup;
                }

                if(ParentServerGroup == null)
                {
                    ParentTenant = value as Tenant;
                }
            }
        }
        
        public virtual Customer ParentCustomer { get; set; }
        public virtual Metric ParentMetric { get; set; }
        public virtual MetricInstance ParentMetricInstance { get; set; }
        public virtual Server ParentServer { get; set; }
        public virtual ServerGroup ParentServerGroup { get; set; }
        public virtual Tenant ParentTenant { get; set; }
        #endregion

        public virtual IList<MetricThreshold> MetricThresholds { get; set; }
        public virtual IList<Schedule> Schedules { get; set; }
        public bool IsTemplate { get; set; }

        #endregion

        #region ctor
        [Obsolete("Use static NewMetricConfiguration factory instead")]
        public MetricConfiguration()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static MetricConfiguration NewMetricConfiguration(IMetricConfigurationParent parent, Metric metric, string name)
        {
            var entity = new MetricConfiguration() { Parent = parent, Metric = metric, Name = name, Id = Guid.NewGuid() };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            MetricThresholds = new List<MetricThreshold>();
            Schedules = new List<Schedule>();
        }
        #endregion

        #region Methods
        #endregion
    }
}
