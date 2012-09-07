using System;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Application.Interface
{
    public interface IIncidentService
    {
        void AddMetricThresholdHistory(DateTime timestamp, Guid metricInstanceId, Guid metricThresholdId, float percentage = 0, long value = 0, string matchValue = null, string additionalData = null);
        float GetAverageValue(Guid metricInstanceId, Guid metricThresholdId, int minutes);
        float GetAveragePercentage(Guid metricInstanceId, Guid metricThresholdId, int minutes);
        int GetCount(Guid metricInstanceId, Guid metricThresholdId, int minutes);
        bool HasOpenIncident(Guid metricInstanceId);
        bool HasOpenIncident(Guid metricInstanceId, string additionalData);
        string OpenIncident(string body, Guid metricInstanceId, int priority, string summary, string additionalData);
    }
}
