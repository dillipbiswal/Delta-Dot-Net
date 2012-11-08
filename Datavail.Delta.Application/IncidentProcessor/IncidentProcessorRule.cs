using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using System.Linq;

namespace Datavail.Delta.Application.IncidentProcessor
{
    public abstract class IncidentProcessorRule : IIncidentProcessorRule
    {

        protected readonly IIncidentService IncidentService;
        protected readonly IServerService ServerService;

        protected readonly XDocument DataCollection;
        protected string PercentageTypeLabel;
        protected float PercentageTypeValue;
        protected string ValueTypeLabel;
        protected long ValueTypeValue;
        protected string MatchTypeLabel;
        protected string MatchTypeValue;

        protected string XmlMatchString;

        public string AdditionalData { get; set; }
        public string IncidentMesage { get; set; }
        public int IncidentPriority { get; set; }
        public string IncidentSummary { get; set; }
        public string Label { get; set; }
        public Guid MetricInstanceId { get; set; }
        public MetricInstance MetricInstance { get; set; }
        public Guid ServerId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string RuleName { get; set; }
        public IEnumerable<MetricThreshold> Thresholds { get; set; }
        public DateTime Timestamp { get; set; }

        protected IncidentProcessorRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
        {

            IncidentService = incidentService;
            DataCollection = dataCollection;
            ServerService = serverService;

            IncidentPriority = 1;
        }

        protected abstract void ParseDataCollection(XDocument dataCollection);

        protected virtual void SetupMatchParams()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name == XmlMatchString)
            {
                GetCommonAttributes();
                GetThresholds(MetricInstanceId);
                ParseDataCollection(DataCollection);
            }
        }

        public virtual bool IsMatch()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            foreach (var metricThreshold in Thresholds)
            {
                //Setup Common Items
                var isPercentageType = metricThreshold.ThresholdValueType == ThresholdValueType.Percentage;
                var isCountType = metricThreshold.ThresholdComparisonFunction == ThresholdComparisonFunction.Value;
                var isAverageType = metricThreshold.ThresholdComparisonFunction == ThresholdComparisonFunction.Average;
                var isMatchType = metricThreshold.ThresholdComparisonFunction == ThresholdComparisonFunction.Match;
                var isSingleMatchType = metricThreshold.NumberOfOccurrences <= 1;

                var metricTypeDescription = isPercentageType ? PercentageTypeLabel : ValueTypeLabel;
                double metricValue = isPercentageType ? PercentageTypeValue : ValueTypeValue;


                if (isCountType)
                {
                    if (metricValue >= metricThreshold.FloorValue && metricValue <= metricThreshold.CeilingValue)
                    {
                        if (isPercentageType)
                        {
                            IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, percentage: (float)metricValue);
                        }
                        else
                        {
                            IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, value: (long)metricValue);
                        }
                        if (isSingleMatchType)
                        {
                            IncidentPriority = (int)metricThreshold.Severity;
                            IncidentMesage = FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold);
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            return true;
                        }
                        else
                        {
                            var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);
                            IncidentPriority = (int)metricThreshold.Severity;
                            IncidentMesage = FormatCountServiceDeskMessage(count, metricTypeDescription, metricThreshold);
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            if (count >= metricThreshold.NumberOfOccurrences) return true;
                        }
                    }
                }

                if (isAverageType)
                {
                    if (isPercentageType)
                    {
                        IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, percentage: (float)metricValue);
                    }
                    else
                    {
                        IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, value: (long)metricValue);
                    }

                    var average = isPercentageType
                                      ? IncidentService.GetAveragePercentage(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod)
                                      : IncidentService.GetAverageValue(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);

                    if (!float.IsNaN(average) && average >= metricThreshold.FloorValue && average <= metricThreshold.CeilingValue)
                    {
                        IncidentPriority = (int)metricThreshold.Severity;
                        IncidentMesage = FormatAverageServiceDeskMessage(average, metricTypeDescription, metricThreshold);
                        IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                        return true;
                    }
                }

                if (isMatchType)
                {
                    if (Regex.IsMatch(MatchTypeValue, metricThreshold.MatchValue))
                    {
                        IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, matchValue: MatchTypeValue);

                        if (isSingleMatchType)
                        {
                            IncidentPriority = (int)metricThreshold.Severity;
                            IncidentMesage = FormatMatchServiceDeskMessage(metricThreshold);
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            return true;
                        }
                        else
                        {
                            var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);
                            IncidentPriority = (int)metricThreshold.Severity;
                            IncidentMesage = FormatMatchCountServiceDeskMessage(count, metricThreshold);
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            if (count >= metricThreshold.NumberOfOccurrences) return true;
                        }
                    }
                }
            }

            //None of the thresholds match, so don't open an incident
            return false;
        }


        protected virtual string FormatAverageServiceDeskMessage(float average, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            return string.Empty;
        }

        protected virtual string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            return string.Empty;
        }

        protected virtual string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            return string.Empty;
        }

        protected virtual string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            return string.Empty;
        }

        protected virtual string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            return string.Empty;
        }

        protected virtual string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            return string.Empty;
        }


        private void GetCommonAttributes()
        {
            if (DataCollection.Root != null)
            {
                var xTimeStamp = DataCollection.Root.Attribute("timestamp");
                if (xTimeStamp != null)
                {
                    var culture = CultureInfo.InvariantCulture;
                    Timestamp = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(xTimeStamp.Value));
                }

                var xMetricInstanceId = DataCollection.Root.Attribute("metricInstanceId");
                if (xMetricInstanceId != null)
                {
                    MetricInstanceId = Guid.Parse(xMetricInstanceId.Value);
                    MetricInstance = ServerService.GetMetricInstance(MetricInstanceId);
                }

                var xLabel = DataCollection.Root.Attribute("label");
                if (xLabel != null)
                {
                    Label = xLabel.Value;
                }

                var serverInfo = ServerService.GetServerInfoFromMetricInstanceId(MetricInstanceId);
                var serverInfoXml = XDocument.Parse(serverInfo);

                if (serverInfoXml.Root != null)
                {
                    var xServerId = serverInfoXml.Root.Attribute("serverId");
                    if (xServerId != null)
                    {
                        ServerId = Guid.Parse(xServerId.Value);
                    }

                    var xHostname = serverInfoXml.Root.Attribute("hostName");
                    if (xHostname != null)
                    {
                        Hostname = xHostname.Value;
                    }

                    var xIpAddress = serverInfoXml.Root.Attribute("ipAddress");
                    if (xIpAddress != null && !string.IsNullOrEmpty(xIpAddress.Value))
                    {
                        IpAddress = xIpAddress.Value;
                    }
                    else
                    {
                        IpAddress = "n/a";
                    }
                }
            }
        }

        private void GetThresholds(Guid metricInstanceId)
        {
            Thresholds = ServerService.GetThresholds(metricInstanceId);
        }
    }
}
