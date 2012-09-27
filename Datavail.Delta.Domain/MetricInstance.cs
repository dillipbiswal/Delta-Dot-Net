using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Datavail.Delta.Domain
{
    public class MetricInstance : DomainBase, IMetricConfigurationParent, IMaintenanceWindowParent
    {
        #region Fields
        #endregion

        #region Properties
        public string Data { get; set; }
        public string Label { get; set; }
        public virtual Metric Metric { get; set; }
        public virtual IList<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual IList<MaintenanceWindow> MaintenanceWindows { get; set; }
        public virtual Server Server { get; set; }
        public virtual Status Status { get; set; }
        public virtual DatabaseInstance DatabaseInstance { get; set; }
        public virtual Database Database { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewMetricInstance factory instead")]
        public MetricInstance()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static MetricInstance NewMetricInstance(string label, Metric metric)
        {
            var entity = new MetricInstance() { Label = label, Metric = metric };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            Data = string.Empty;

            Label = string.Empty;
            MetricConfigurations = new List<MetricConfiguration>();
            MaintenanceWindows = new List<MaintenanceWindow>();
        }
        #endregion

        #region Methods
        #endregion
    }
}
