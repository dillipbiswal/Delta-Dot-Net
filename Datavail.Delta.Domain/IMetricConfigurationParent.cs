using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public interface IMetricConfigurationParent
    {
        Guid Id { get; set; }
        Status Status { get; set; }
        IList<MetricConfiguration> MetricConfigurations { get; set; }
    }
}