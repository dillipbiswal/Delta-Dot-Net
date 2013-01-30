using System;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Application.Interface
{
    public interface IIncidentProcessorRule
    {
        string AdditionalData { get; set; }
        string IncidentMesage { get; set; }
        int IncidentPriority { get; set; }
        string IncidentSummary { get; set; }
        bool IsMatch();
        MetricInstance MetricInstance { get; set; }
        string RuleName { get; set; }
        Guid ServerId { get; }
    }
}