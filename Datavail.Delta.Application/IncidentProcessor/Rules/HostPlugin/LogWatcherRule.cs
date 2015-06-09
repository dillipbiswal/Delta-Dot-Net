using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin
{
    public sealed class LogWatcherMatchStatus : IncidentProcessorRule
    {
        private string _fileName;
        private string _matchingLine;
        private string _matchingExpression;

        private const string ServiceDeskMatchMessage = "The Delta monitoring application has detected that the file {0} contains {1} (metricInstanceId: {2}).\n\nAgent Timestamp (UTC): {7}\nMatch Value: {3}\nMetric Threshold: {4}\nServer: {5}\nIp Address: {6}\n";
        private const string ServiceDeskMatchCountMessage = "The Delta monitoring application has detected that the file {0} contains {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nAgent Timestamp (UTC): {9}\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Log File {2} matched {3}";

        public LogWatcherMatchStatus( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Log Watcher Status Match";
            XmlMatchString = "LogWatcherPluginOutput";

            SetupMatchParams();
        }

        public override bool IsMatch()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            var matchFound = false;
            var incidentDetailMesages = new List<string>();

            const string timeStampRegEx = "[0-9]{1,4}-[0-9]{1,2}-[0-9]{1,2} [0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}.[0-9]{1,2} [a-zA-Z0-9_-]{3,14} {1,7}";
            var data = string.Empty;

            if (Regex.IsMatch(_matchingLine, timeStampRegEx))
            {
                data = Regex.Replace(_matchingLine, timeStampRegEx, string.Empty);
            }

            AdditionalData = string.Format("<AdditionalData><LastMatchingLine>{0}</LastMatchingLine></AdditionalData>", data);

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
                            IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, percentage: (float)metricValue, additionalData: AdditionalData);
                        }
                        else
                        {
                            IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, value: (long)metricValue, additionalData: AdditionalData);
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
                        IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, percentage: (float)metricValue, additionalData: AdditionalData);
                    }
                    else
                    {
                        IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id, value: (long)metricValue, additionalData: AdditionalData);
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
                        IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id,
                                                                    matchValue: MatchTypeValue, additionalData: AdditionalData);

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

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            MatchTypeValue = _matchingLine;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchMessage, _fileName, _matchingLine, MetricInstanceId, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress,Timestamp);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            _matchingExpression = metricThreshold.MatchValue;
            var message = string.Format(ServiceDeskMatchCountMessage, _fileName, _matchingLine, MetricInstanceId, count, metricThreshold.TimePeriod, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress,Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname, _fileName, _matchingExpression);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            Guard.IsNotNull(dataCollection.Root.Attribute("matchingLine"), "matchingLine");
            
            _matchingLine = dataCollection.Root.Attribute("matchingLine").Value;

            if(dataCollection.Root.Attribute("fileName") != null)
                _fileName = dataCollection.Root.Attribute("fileName").Value;
        }   
    }
}