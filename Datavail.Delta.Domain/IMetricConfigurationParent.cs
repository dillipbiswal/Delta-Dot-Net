using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public interface IMetricConfigurationParent
    {
        Guid Id { get; set; }
        StatusWrapper Status { get; set; }
        IList<MetricConfiguration> MetricConfigurations { get; set; }
    }
}