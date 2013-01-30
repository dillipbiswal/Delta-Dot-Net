using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class DatabaseServerBlockingRule : IncidentProcessorRule
    {
        List<NameValueCollection> nodes = new List<NameValueCollection>();

        private string _database;
        private string _requestSessionCommand;
        private string _requestSessionId;
        private long _waitingDurationSec;
        private string _blockingId;
        private string _blockingCommand;
        private string _instanceName;

        private const string SERVICE_DESK_MESSAGE_HEADER = "The Delta monitoring application has detected the following blocking command(s).";
        private const string SERVICE_DESK_MESSAGE = "(metricInstanceId: {0}).\n\nInstance Name: {1}\nBlocking Command: {2}\nBlocking Id: {9} \nRequest Session Command: {3}\nRequest Session Id: {10}\nDatabase: {4}\n\nMatch Value: {5}\n\nAgent Timestamp (UTC): {11}\nMetric Threshold: {6}\nServer: {7}\nIp Address: {8}\n";
        private const string SERVICE_DESK_MATCH_MESSAGE = "(metricInstanceId: {0}).\n\nInstance Name: {1}\nBlocking Command: {2}\nBlocking Id: {3} \nRequest Session Command: {4}\nRequest Session Id: {5}\nDatabase: {6}\n\nMatch Value: {7}\n\nAgent Timestamp (UTC): {11}\nMetric Threshold: {8}\nServer: {9}\nIp Address: {10}\n";
        private const string SERVICE_DESK_MESSAGE_COUNT = "(metricInstanceId: {0}). This has occurred {1} times in the last {2} minutes.\n\nInstance Name: {3}\nBlocking Command: {4}\nBlocking Id: {5} \nRequest Session Command: {6}\nRequest Session Id: {7}\nDatabase: {8}\n\nMatch Value: {9}\n\nAgent Timestamp (UTC): {13}\nMetric Threshold: {10}\nServer: {11}\nIp Address: {12}\n";
        private const string SERVICE_DESK_SUMMARY = "P{0}/{1}/Blocking Command(s) detected.";

        public DatabaseServerBlockingRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Database Server Blocking Threshold Breach";
            XmlMatchString = "DatabaseServerBlockingPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();
            //MatchTypeValue = _waitingDurationSec.ToString();
        }

        public override bool IsMatch()
        {

            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            var matchFound = false;
            List<string> IncidentMesages = new List<string>();

            foreach (NameValueCollection node in nodes)
            {
                _waitingDurationSec = 0;
                long.TryParse(node["WaitingDurationSec"], out _waitingDurationSec);
                _blockingId = node["BlockingId"];
                _blockingCommand = node["BlockingCommand"];
                _database = node["Database"];
                _requestSessionId = node["RequestSessionId"];
                _requestSessionCommand = node["RequestSessionCommand"];
                _instanceName = node["InstanceName"];

                ValueTypeValue = _waitingDurationSec;
                ValueTypeLabel = "Database Server Blocking Status";

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
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = SERVICE_DESK_MESSAGE_HEADER + Environment.NewLine;
                                IncidentMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = SERVICE_DESK_MESSAGE_HEADER + Environment.NewLine;
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
                            //IncidentMesage = FormatAverageServiceDeskMessage(average, metricTypeDescription, metricThreshold);
                            IncidentMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
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
                                IncidentMesage = SERVICE_DESK_MESSAGE_HEADER + Environment.NewLine;
                                IncidentMesages.Add(FormatMatchServiceDeskMessage(metricThreshold));
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id,
                                                                      metricThreshold.TimePeriod);
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = SERVICE_DESK_MESSAGE_HEADER + Environment.NewLine;
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
            var message = string.Format(SERVICE_DESK_MATCH_MESSAGE, MetricInstanceId, _instanceName, _blockingCommand, _blockingId, _requestSessionCommand, _requestSessionId, _database, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_MESSAGE, MetricInstanceId, _instanceName, _blockingCommand, _blockingId, _requestSessionCommand, _requestSessionId, _database, metricThreshold.MatchValue, metricThreshold.Id, Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MESSAGE_COUNT, MetricInstanceId, count,
                                        metricThreshold.TimePeriod, _instanceName, _blockingCommand, _blockingId,
                                        _requestSessionCommand, _requestSessionId, _database, metricThreshold.MatchValue,
                                        metricThreshold.Id, Hostname, IpAddress, Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(SERVICE_DESK_SUMMARY, IncidentPriority, Hostname);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataResultCollection)
        {

            foreach (XElement dataCollection in dataResultCollection.Element("DatabaseServerBlockingPluginOutput").Elements("BlockingStatus"))
            {
                NameValueCollection collection = new NameValueCollection();

                var xWaitingDurationSec = dataCollection.Attribute("waitingDurationSec");
                if (xWaitingDurationSec != null)
                {
                    _waitingDurationSec = long.Parse(xWaitingDurationSec.Value);
                    collection.Add("WaitingDurationSec", _waitingDurationSec.ToString());
                }

                var xDatabase = dataCollection.Attribute("database");
                if (xDatabase != null)
                {
                    _database = xDatabase.Value;
                    collection.Add("Database", _database);
                }

                var xRequestSessionId = dataCollection.Attribute("requestSessionId");
                if (xRequestSessionId != null)
                {
                    _requestSessionId = xRequestSessionId.Value;
                    collection.Add("RequestSessionId", _requestSessionId);
                }

                var xResuestSesssionCommand = dataCollection.Attribute("requestSessionCommand");
                if (xResuestSesssionCommand != null)
                {
                    _requestSessionCommand = xResuestSesssionCommand.Value;
                    collection.Add("RequestSessionCommand", _requestSessionCommand);
                }

                var xBlockingId = dataCollection.Attribute("blockingId");
                if (xBlockingId != null)
                {
                    _blockingId = xBlockingId.Value;
                    collection.Add("BlockingId", _blockingId);
                }

                var xBlockingCommand = dataCollection.Attribute("blockingCommand");
                if (xBlockingCommand != null)
                {
                    _blockingCommand = xBlockingCommand.Value;
                    collection.Add("BlockingCommand", _blockingCommand);
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