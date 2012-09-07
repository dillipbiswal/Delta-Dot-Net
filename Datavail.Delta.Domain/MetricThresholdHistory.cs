using System;

namespace Datavail.Delta.Domain
{
    public class MetricThresholdHistory : DomainBase
    {
        public string AdditionalData { get; set; }
        public string MatchValue { get; set; }
        public virtual MetricInstance MetricInstance { get; set; }
        public virtual MetricThreshold MetricThreshold { get; set; }
        public float Percentage { get; private set; }
        public DateTime Timestamp { get; private set; }
        public long Value { get; private set; }


        #region ctor
        [Obsolete("Use static NewMetricThresholdHistory factory instead")]
        public MetricThresholdHistory()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static MetricThresholdHistory NewMetricThresholdHistory(DateTime timestamp, MetricInstance metricInstace, MetricThreshold metricThreshold, float percentage = 0, long value = 0, string matchValue = null)
        {
            var entity = new MetricThresholdHistory { MatchValue = matchValue ?? string.Empty, MetricInstance = metricInstace, MetricThreshold = metricThreshold, Percentage = percentage, Timestamp = timestamp, Value = value };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {

        }
        #endregion
    }
}
