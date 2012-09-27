using System;

namespace Datavail.Delta.Domain
{
    public class MetricThreshold : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public float CeilingValue { get; set; }
        public float FloorValue { get; set; }
        public string MatchValue { get; set; }
        public virtual MetricConfiguration MetricConfiguration { get; set; }
        public int NumberOfOccurrences { get; set; }
        public virtual Severity Severity { get; set; }
        public virtual ThresholdComparisonFunction ThresholdComparisonFunction { get; set; }
        public virtual ThresholdValueType ThresholdValueType { get; set; }
        public int TimePeriod { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewMetricThreshold factory instead")]
        public MetricThreshold()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static MetricThreshold NewMetricThreshold(float ceilingValue, float floorValue, string matchValue, int numberOfOccurrences, Severity severity, ThresholdComparisonFunction thresholdComparisonFunction, ThresholdValueType thresholdValueType, int timePeriod, Guid? id = null)
        {
            var entity = new MetricThreshold() { Severity = severity, FloorValue = floorValue, CeilingValue = ceilingValue, NumberOfOccurrences = numberOfOccurrences, ThresholdComparisonFunction = thresholdComparisonFunction, ThresholdValueType = thresholdValueType, TimePeriod = timePeriod, MatchValue = matchValue };
            if (id != null) entity.Id = (Guid)id;
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion
    }
}
