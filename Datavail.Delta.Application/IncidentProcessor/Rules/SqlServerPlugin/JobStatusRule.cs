using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class JobStatusRule : IncidentProcessorRule
    {
        private readonly List<NameValueCollection> _nodes = new List<NameValueCollection>();
        private string _jobStatus;
        private string _jobName;
        private string _message;
        private string _instanceName;
        private string _stepId;
        private string _stepName;
        private string _runDuration;
        private string _runDate;
        private string _runTime;
        private string _retriesAttempted;

        private const string SERVICE_DESK_MESSAGE_HEADER = "The Delta monitoring application has detected that the job {0} is reporting a status of {1} (metricInstanceId: {2}).\n\nAgent Timestamp: {4}\nInstance Name: {3}\n\nStep Details\n\n";
        private const string SERVICE_DESK_JOB_STEP_MESSAGE = "Step Id: {0}\nStep Name: {1}\nRun Date:{2}\nRun Time: {3}\nRun Duration: {4}\nRetries Attempted: {5}\nMessage: {6}\n\n";
        private const string SERVICE_DESK_MATCH_MESSAGE = "Agent Timestamp: {5}\nMetric Threshold: {0}\nMatch Value: {1}\nServer: {2} ({3})\nIp Address: {4}\n";
        private const string SERVICE_DESK_MATCH_COUNT_MESSAGE = "The Delta monitoring application has detected that the job {0} is reporting a status of {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nInstance Name: {5}\n\nStep Details\n";
        private const string SERVICE_DESK_SUMMARY = "P{0}/{1}/Job {4}/{2} is {3}";

        public JobStatusRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Database Job Status Match";
            XmlMatchString = "DatabaseJobStatusPluginOutput";

            SetupMatchParams();
        }

        public override bool IsMatch()
        {
            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;

            var matchFound = false;
            var incidentDetailMesages = new List<string>();

            foreach (var node in _nodes)
            {
                _jobStatus = node["JobStatus"];
                _jobName = node["JobName"];
                _message = node["Message"];
                _instanceName = node["InstanceName"];
                _stepId = node["StepId"];
                _stepName = node["StepName"];
                _runDate = node["RunDate"];
                _runDuration = node["RunDuration"];
                _retriesAttempted = node["RetriesAttempted"];
                _runTime = node["RunTime"];

                AdditionalData = string.Format("<AdditionalData><LastRunDate>{0}</LastRunDate><LastRunTime>{1}</LastRunTime></AdditionalData>", _runDate, _runTime);

                MatchTypeValue = _jobStatus;

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
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = FormatServiceDeskMessageHeader() + Environment.NewLine;
                                incidentDetailMesages.Add(FormatServiceDeskJobStepMessage());
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = FormatServiceDeskMessageHeader() + Environment.NewLine;
                                incidentDetailMesages.Add(FormatServiceDeskJobStepMessage());
                                if (count >= metricThreshold.NumberOfOccurrences) matchFound = true;
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
                            incidentDetailMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            matchFound = true;
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
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = FormatServiceDeskMessageHeader() + Environment.NewLine;
                                incidentDetailMesages.Add(FormatServiceDeskJobStepMessage());
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id,
                                                                      metricThreshold.TimePeriod);
                                IncidentPriority = (int)metricThreshold.Severity;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = FormatServiceDeskCountMessageHeader(count, metricThreshold) + Environment.NewLine;
                                incidentDetailMesages.Add(FormatServiceDeskJobStepMessage());

                                if (count >= metricThreshold.NumberOfOccurrences) matchFound = true;
                            }
                        }
                    }
                }

            }


            if (matchFound)
            {
                foreach (var message in incidentDetailMesages)
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

        private string FormatServiceDeskMessageHeader()
        {
            var message = string.Format(SERVICE_DESK_MESSAGE_HEADER, _jobName, _jobStatus, MetricInstanceId, _instanceName, Timestamp);
            return message;
        }

        private string FormatServiceDeskJobStepMessage()
        {
            var message = string.Format(SERVICE_DESK_JOB_STEP_MESSAGE, _stepId, _stepName, _runDate, _runTime, _runDuration, _retriesAttempted, _message);
            return message;

        }

        private string FormatServiceDeskCountMessageHeader(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_COUNT_MESSAGE, _jobName, _jobStatus, MetricInstanceId, count, metricThreshold.TimePeriod, _instanceName);
            return message;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_MESSAGE, _jobName, _jobStatus, MetricInstanceId, _jobStatus, metricThreshold.Id, Timestamp);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(SERVICE_DESK_MATCH_COUNT_MESSAGE, _jobName, _jobStatus, MetricInstanceId, count, metricThreshold.TimePeriod, _jobStatus, Timestamp);
            return message;
        }

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(SERVICE_DESK_SUMMARY, IncidentPriority, Hostname, _jobName, _jobStatus, _instanceName);
            return message;
        }

        protected override void ParseDataCollection(XDocument dataResultCollection)
        {
            Guard.IsNotNull(dataResultCollection.Element(XmlMatchString), "DataResultCollection");
            var xElement = dataResultCollection.Element(XmlMatchString);

            if (xElement != null)
                foreach (var dataCollection in xElement.Elements("JobStatus"))
                {
                    var collection = new NameValueCollection();

                    var xJobStatus = dataCollection.Attribute("jobStatus");
                    if (xJobStatus != null)
                    {
                        collection.Add("JobStatus", xJobStatus.ToString());
                    }

                    var xJobName = dataCollection.Attribute("jobName");
                    if (xJobName != null)
                    {
                        collection.Add("JobName", xJobName.Value);
                    }

                    var xMessage = dataCollection.Attribute("message");
                    if (xMessage != null)
                    {
                        collection.Add("Message", xMessage.Value);
                    }

                    var xStepId = dataCollection.Attribute("stepId");
                    if (xStepId != null)
                    {
                        collection.Add("StepId", xStepId.Value);
                    }

                    var xStepName = dataCollection.Attribute("stepName");
                    if (xStepName != null)
                    {
                        collection.Add("StepName", xStepName.Value);
                    }

                    var xRunDate = dataCollection.Attribute("runDate");
                    if (xRunDate != null)
                    {
                        collection.Add("RunDate", xRunDate.Value);
                    }

                    var xRunTime = dataCollection.Attribute("runTime");
                    if (xRunTime != null)
                    {
                        collection.Add("RunTime", xRunTime.Value);
                    }

                    var xRunDuration = dataCollection.Attribute("runDuration");
                    if (xRunDuration != null)
                    {
                        collection.Add("RunDuration", xRunDuration.Value);
                    }

                    var xRetriesAttempted = dataCollection.Attribute("retriesAttempted");
                    if (xRetriesAttempted != null)
                    {
                        collection.Add("RetriesAttempted", xRetriesAttempted.Value);
                    }

                    var xInstanceName = dataCollection.Attribute("instanceName");
                    if (xInstanceName != null)
                    {
                        _instanceName = xInstanceName.Value;
                        collection.Add("InstanceName", xInstanceName.Value);
                    }

                    _nodes.Add(collection);
                }
        }
    }
}