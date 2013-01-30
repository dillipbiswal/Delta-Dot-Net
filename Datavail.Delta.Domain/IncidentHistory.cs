using System;

namespace Datavail.Delta.Domain
{
    public class IncidentHistory : DomainBase
    {
        public string AdditionalData { get; set; }
        public DateTime? CloseTimestamp { get; set; }
        public string IncidentNumber { get; set; }
        public DateTime? IncidentTimestamp { get; set; }
        public virtual MetricInstance MetricInstance { get; set; }
        public DateTime OpenTimestamp { get; set; }

        public IncidentHistory()
        {
            OpenTimestamp = DateTime.UtcNow;
            IncidentNumber = "-1";
        }

        public IncidentHistory(string additionalData)
        {
            OpenTimestamp = DateTime.UtcNow;
            IncidentNumber = "-1";
            AdditionalData = additionalData;
        }
    }
}
