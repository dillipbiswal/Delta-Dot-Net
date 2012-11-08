using System;
using System.Linq;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Infrastructure.Util;
using Datavail.Delta.Repository.Interface;


namespace Datavail.Delta.Application
{
    public class IncidentService : IIncidentService
    {
        private readonly IServiceDesk _serviceDesk;
        private readonly IIncidentRepository _repository;

        public IncidentService(IServiceDesk serviceDesk, IIncidentRepository repository)
        {
            _serviceDesk = serviceDesk;
            _repository = repository;
        }

        public void AddMetricThresholdHistory(DateTime timestamp, Guid metricInstanceId, Guid metricThresholdId, float percentage = 0, long value = 0, string matchValue = null, string additionalData = null)
        {
            var metricInstance = _repository.GetByKey<MetricInstance>(metricInstanceId);
            var metricThreshold = _repository.GetByKey<MetricThreshold>(metricThresholdId);
            var history = MetricThresholdHistory.NewMetricThresholdHistory(timestamp, metricInstance, metricThreshold, percentage, value, matchValue);

            _repository.Add(history);
            _repository.UnitOfWork.SaveChanges();
        }

        public float GetAverageValue(Guid metricInstanceId, Guid metricThresholdId, int samples)
        {
            var count = GetCount(metricInstanceId, metricThresholdId, 60 * 24);
            if (count < samples) return float.NaN;

            var average = (from h in _repository.Find(new Specification<MetricThresholdHistory>(h => h.MetricInstance.Id == metricInstanceId && h.MetricThreshold.Id == metricThresholdId))
                           orderby h.Timestamp
                           group h by h.MetricInstance.Id
                               into a
                               select new { AverageValue = a.Average(h => h.Value) }).Take(samples);

            var firstOrDefault = average.FirstOrDefault();
            return firstOrDefault != null ? (long)firstOrDefault.AverageValue : float.NaN;
        }

        public float GetAveragePercentage(Guid metricInstanceId, Guid metricThresholdId, int samples)
        {
            var count = GetCount(metricInstanceId, metricThresholdId, 60 * 24);
            if (count < samples) return float.NaN;

            var average = (from h in _repository.Find(new Specification<MetricThresholdHistory>(h => h.MetricInstance.Id == metricInstanceId && h.MetricThreshold.Id == metricThresholdId))
                           group h by h.MetricInstance.Id
                               into a
                               select new { AverageValue = a.Average(h => h.Percentage) }).Take(samples);

            var firstOrDefault = average.FirstOrDefault();
            return firstOrDefault != null ? firstOrDefault.AverageValue : float.NaN;
        }

        public int GetCount(Guid metricInstanceId, Guid metricThresholdId, int minutes)
        {
            var date = DateTime.UtcNow.AddMinutes(-1 * minutes);
            var count = _repository.Find(new Specification<MetricThresholdHistory>(h => h.MetricInstance.Id == metricInstanceId && h.MetricThreshold.Id == metricThresholdId && h.Timestamp >= date)).Count();
            return count;
        }

        public bool HasOpenIncident(Guid metricInstanceId) { return _repository.HasOpenIncident(metricInstanceId); }
        public bool HasOpenIncident(Guid metricInstanceId, string additionalData) { return _repository.HasOpenIncident(metricInstanceId, additionalData); }

        public string OpenIncident(string body, Guid metricInstanceId, int priority, string summary, string additionalData = null)
        {
            var metricInstance = _repository.GetByKey<MetricInstance>(metricInstanceId);


            if (!string.IsNullOrEmpty(additionalData))
            {
                //Make sure that there isn't an open ticket for the metric instance or a closed ticket matching the additionalData
                if (!HasOpenIncident(metricInstance.Id) && !HasOpenIncident(metricInstance.Id, additionalData))
                {
                    var incidentHistory = new IncidentHistory { MetricInstance = metricInstance, OpenTimestamp = DateTime.UtcNow, AdditionalData = additionalData };

                    _repository.Add(incidentHistory);
                    _repository.UnitOfWork.SaveChanges();

                    var xml = GetOpenIncidentXml(metricInstance.Server.Customer.ServiceDeskData, summary, body, priority);
                    var ticket = _serviceDesk.OpenIncident(xml);
                    incidentHistory.IncidentNumber = ticket;
                    incidentHistory.IncidentTimestamp = DateTime.UtcNow;

                    _repository.Update(incidentHistory);
                    _repository.UnitOfWork.SaveChanges();

                    return ticket;
                }
            }
            else
            {
                //Allow duplicate incidents to be opened by LogWatcher, but not by others
                if (!HasOpenIncident(metricInstance.Id) || metricInstance.Metric.AdapterClass == "LogWatcherPlugin")
                {
                    var incidentHistory = new IncidentHistory { MetricInstance = metricInstance, OpenTimestamp = DateTime.UtcNow };

                    _repository.Add(incidentHistory);
                    _repository.UnitOfWork.SaveChanges();

                    var xml = GetOpenIncidentXml(metricInstance.Server.Customer.ServiceDeskData, summary, body, priority);
                    var ticket = _serviceDesk.OpenIncident(xml);
                    incidentHistory.IncidentNumber = ticket;
                    incidentHistory.IncidentTimestamp = DateTime.UtcNow;

                    _repository.Update(incidentHistory);
                    _repository.UnitOfWork.SaveChanges();

                    return ticket;
                }
            }

            return string.Empty;
        }

        private static string GetOpenIncidentXml(string serviceDeskData, string summary, string body, int priority)
        {
            Guard.IsNotNull(serviceDeskData, "ServiceDeskData cannot be null");

            var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"),
                                    new XElement("IncidentOpenXml",
                                                 new XAttribute("IncidentBody", body),
                                                 new XAttribute("IncidentSummary", summary),
                                                 new XAttribute("IncidentPriority", priority),
                                                 new XElement(XElement.Parse(serviceDeskData))));
            return xml.ToString();
        }
    }
}
