using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Domain
{
    public class Metric : DomainBase, IMetricConfigurationParent, IMaintenanceWindowParent
    {
        #region Fields
        #endregion

        #region Properties
        [Required]
        public string AdapterAssembly { get; set; }
        [Required]
        public string AdapterVersion { get; set; }
        [Required]
        public string AdapterClass { get; set; }
        [Required]
        public string Name { get; set; }
        public DatabaseVersionWrapper DatabaseVersion { get; set; }
        public virtual IList<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual IList<MaintenanceWindow> MaintenanceWindows { get; set; }
        public virtual MetricTypeWrapper MetricType { get; set; }
        public virtual MetricThresholdTypeWrapper MetricThresholdType { get; set; }
        public virtual StatusWrapper Status { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewMetric factory instead")]
        public Metric()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static Metric NewMetric(string adapterAssembly, string adapterClass, string adapterVersion, string name)
        {
            var entity = new Metric() { AdapterAssembly = adapterAssembly, AdapterClass = adapterClass, AdapterVersion = adapterVersion, Name = name };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            MetricConfigurations = new List<MetricConfiguration>();
            MaintenanceWindows = new List<MaintenanceWindow>();
        }
        #endregion

        #region Methods
        #endregion
    }
}
