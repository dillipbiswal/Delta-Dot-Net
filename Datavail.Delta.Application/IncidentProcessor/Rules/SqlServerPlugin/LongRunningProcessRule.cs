using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class LongRunningProcessRule : IncidentProcessorRule
    {
        List<NameValueCollection> nodes = new List<NameValueCollection>();

        private long _longRunningProcessThreshold;
        private string _currentRunTime;
        private string _spid;
        private string _programName;
        private string _lastBatch;
        private string _sqlStatements;
        private string _instanceName;

        private const string ServiceDeskMessageHeader = "The Delta monitoring application has detected the following Long Running Process(s).";
        private const string ServiceDeskMessage = "(metricInstanceId: {0}).\n\nInstance Name: {1}\nProgram Name: {2}\nSQL Statement: {3}\nSPID: {4}\n\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string ServiceDeskMatchMessage = "(metricInstanceId: {0}).\n\nInstance Name: {1}\nProgram Name: {2}\nSQL Statement: {3}\nSPID: {4}\n\nMatch Value: {5}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string ServiceDeskMessageCount = "(metricInstanceId: {0}). This has occurred {1} times in the last {2} minutes.\n\nInstance Name: {3}\nProgram Name: {4}\nSQL Statement: {5}\nSPID: {6}\n\nMatch Value: {7}\nMetric Threshold: {8}\nServer: {9}\nIp Address: {10}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Long Running Process(s) detected.";

        public LongRunningProcessRule( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Long Running Process Match";
            XmlMatchString = "DatabaseServerLongRunningProcessPluginOutput";
   
            SetupMatchParams();
        }

        public override bool IsMatch()
        {

            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;
            var matchFound = false;
            List<string> IncidentMesages = new List<string>();

            foreach (NameValueCollection node in nodes)
            {
                _longRunningProcessThreshold = 0;
                long.TryParse(node["LongRunningProcessThreshold"], out _longRunningProcessThreshold);
                _currentRunTime = node["CurrentRunTime"];
                _spid = node["Spid"];
                _programName = node["ProgramName"];
                _lastBatch = node["LastBatch"];
                _sqlStatements = node["SqlStatements"];
                _instanceName = node["InstanceName"];

                ValueTypeValue = _longRunningProcessThreshold;

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
                                IncidentPriority =(int) metricThreshold.Severity;
                                IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesages.Add(FormatCountServiceDeskMessage(count, metricTypeDescription, metricThreshold));
                                if (count >= metricThreshold.NumberOfOccurrences) matchFound = true;
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
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            //IncidentMesage = FormatAverageServiceDeskMessage(average, metricTypeDescription, metricThreshold);
                            IncidentMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                            
                            matchFound = true;
                        }
                    }

                    if (isMatchType)
                    {

                        if (Regex.IsMatch(MatchTypeValue, metricThreshold.MatchValue))
                        {
                            IncidentService.AddMetricThresholdHistory(Timestamp, MetricInstance.Id, metricThreshold.Id,
                                                                       matchValue: MatchTypeValue);

                            if (isSingleMatchType)
                            {
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
                                IncidentMesages.Add(FormatMatchServiceDeskMessage(metricThreshold));

                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id,
                                                                      metricThreshold.TimePeriod);
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
                                IncidentMesages.Add(FormatMatchCountServiceDeskMessage(count, metricThreshold));

                                if (count >= metricThreshold.NumberOfOccurrences) matchFound = true;
                            }
                        }
                    }
                }

            }

            if (matchFound)
            {
                
                foreach (var message in IncidentMesages)
                {
                    IncidentMesage += message;
                    IncidentMesage += "----------------------------------------------------------------------";
                    IncidentMesage += Environment.NewLine;
                    IncidentMesage += Environment.NewLine;
                }


                return true;
            }
            else
            {
                //None of the thresholds match, so don't open an incident
                return false;
            }

        }


        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage,  MetricInstanceId, _instanceName, _programName, _sqlStatements, _spid, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress);
            return message;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMatchMessage, MetricInstanceId, _instanceName, _programName, _sqlStatements, _spid, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageCount, MetricInstanceId, count, metricThreshold.TimePeriod, _instanceName, _programName, _sqlStatements, _spid, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname);
            return message;
        }
        
        protected override void ParseDataCollection(XDocument dataResultCollection)
        {

            NameValueCollection collection = new NameValueCollection();

            foreach (XElement dataCollection in dataResultCollection.Element("DatabaseServerLongRunningProcessPluginOutput").Elements("LongRunningProcessResult"))
            {
                var xLongRunningProcessThreshold = dataCollection.Attribute("longProcessThreshold");
                if (xLongRunningProcessThreshold != null)
                {
                    _longRunningProcessThreshold = long.Parse(xLongRunningProcessThreshold.Value);
                    collection.Add("LongRunningProcessThreshold", _longRunningProcessThreshold.ToString());
                }

                var xCurrentRunTime = dataCollection.Attribute("currentRunTime");
                if (xCurrentRunTime != null)
                {
                    _currentRunTime = xCurrentRunTime.Value;
                    collection.Add("CurrentRunTime", _currentRunTime);
                }

                var xSpid = dataCollection.Attribute("spid");
                if (xSpid != null)
                {
                    _spid = xSpid.Value;
                    collection.Add("Spid", _spid);
                }

                var xProgrameName = dataCollection.Attribute("programName");
                if (xProgrameName != null)
                {
                    _programName = xProgrameName.Value;
                    collection.Add("ProgramName", _programName);
                }

                var xLastBatch = dataCollection.Attribute("lastBatch");
                if (xLastBatch != null)
                {
                    _lastBatch = xLastBatch.Value;
                    collection.Add("LastBatch", _lastBatch);
                }

                var xSqlStatements = dataCollection.Attribute("sqlStatements");
                if (xSqlStatements != null)
                {
                    _sqlStatements = xSqlStatements.Value;
                    collection.Add("SqlStatements", _sqlStatements);
                }


                var xInstanceName = dataCollection.Attribute("instanceName");
                if (xInstanceName != null)
                {
                    _instanceName = xInstanceName.Value;
                    collection.Add("InstanceName", _instanceName);
                }

                nodes.Add(collection);
            }
        }
    }
}