using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;


namespace Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin
{
    public sealed class TransReplicationStatusRule : IncidentProcessorRule
    {
                private string _publisher;
        private string _subscriber;
        private string _publication;
        private long _status;
        private string _subscriberDb;
        private string _type;
        private string _agentName;
        private string _lastAction;
        private string _startTime;
        private string _actionTime;
        private string _duration;
        private string _deliveryRate;

        private string _downloadInserts;
        private string _downloadUpdates;
        private string _downloadDeletes;
        private string _publisherConflicts;
        private string _uploadInserts;
        private string _uploadUpdates;
        private string _uploadDeletes;
        private string _subscriberConflicts;
        private string _errorId;
        private string _jobId;
        private string _localJob;
        private string _profileId;
        private string _agentId;
        private string _offloadEnabled;
        private string _offloadServer;
        private string _subscriberType;
        private string _instanceName;

        List<NameValueCollection> nodes = new List<NameValueCollection>();

        private const string ServiceDeskMessageHeader = "The Delta monitoring application has detected the following Transactional Replication fault(s).";
        private const string ServiceDeskMessage = "(metricInstanceId: {0}).\n\nInstance Name: {18}\nDistribution Host: {1}\nPublisher: {2}\nPublication: {3}\nSubscriber: {4}\nReplication Exception: {5}\nInsert uploads/downloads: {6}\\{7}\nUpdate uploads/downloads: {8}\\{9}\nDelete uploads/downloads: {10}\\{11}\nLast Action: {12}\n\nMetric Threshold: {13}\nMatch Value: {14}\nServer: {15} ({16})\nIp Address: {17}\n";
        private const string ServiceDeskMessageCount = "(metricInstanceId: {0}).\n\nThis has occurred {18} times in the last {19} minutes.\n\nInstance Name: {20}\nDistribution Host: {1}\nPublisher: {2}\nPublication: {3}\nSubscriber: {4}\nReplication Exception: {5}\nInsert uploads/downloads: {6}\\{7}\nUpdate uploads/downloads: {8}\\{9}\nDelete uploads/downloads: {10}\\{11}\nLast Action: {12}\n\nMetric Threshold: {13}\nMatch Value: {14}\nServer: {15} ({16})\nIp Address: {17}\n";
        private const string ServiceDeskSummary = "P{0}/{1}/Transactional Replication threshold breached.";

        public TransReplicationStatusRule( IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            RuleName = "Transactional Replication Threshold Breach";
            XmlMatchString = "DatabaseServerTransactionalReplicationPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            PercentageTypeLabel = "percentage available";
            ValueTypeLabel = "Trans Replication Error.";
            // MatchTypeValue = _status;
        }


        public override bool IsMatch()
        {

            if (DataCollection.Root != null && DataCollection.Root.Name != XmlMatchString)
                return false;
            var matchFound = false;
            List<string> IncidentMesages = new List<string>();

            foreach (NameValueCollection node in nodes)
            {
                //MatchTypeValue = node["Status"];

                _publisher = node["Publisher"];
                _subscriber = node["Subscriber"];
                _publication = node["Publication"];
                _status = 0;
                long.TryParse(node["Status"], out _status);
                _subscriberDb = node["SubscriberDB"];
                _type = node["Type"];
                _agentName = node["AgentName"];
                _lastAction = node["LastAction"];
                _startTime = node["StartTime"];
                _actionTime = node["ActionTime"];
                _duration = node["Duration"];
                _deliveryRate = node["DeliveryRate"];
                _downloadInserts = node["DownloadInserts"];
                _downloadUpdates = node["DownloadUpdates"];
                _downloadDeletes = node["DownloadDeletes"];
                _publisherConflicts = node["PublisherConflicts"];
                _uploadInserts = node["UploadInserts"];
                _uploadUpdates = node["UploadUpdates"];
                _uploadDeletes = node["UploadDeletes"];
                _subscriberConflicts = node["SubscriberConflicts"];
                _errorId = node["ErrorId"];
                _jobId = node["JobId"];
                _localJob = node["LocalJob"];
                _profileId = node["ProfileId"];
                _agentId = node["AgentId"];
                _offloadEnabled = node["OffloadEnabled"];
                _offloadServer = node["OffloadServer"];
                _subscriberType = node["SubscriberType"];
                _instanceName = node["InstanceName"];

                ValueTypeValue = _status;

                foreach (var metricThreshold in Thresholds)
                {
                    //Setup Common Items
                    var isPercentageType = metricThreshold.ThresholdValueType.Enum == ThresholdValueType.Percentage;
                    var isCountType = metricThreshold.ThresholdComparisonFunction.Enum ==
                                      ThresholdComparisonFunction.Value;
                    var isAverageType = metricThreshold.ThresholdComparisonFunction.Enum ==
                                        ThresholdComparisonFunction.Average;
                    var isMatchType = metricThreshold.ThresholdComparisonFunction.Enum ==
                                      ThresholdComparisonFunction.Match;
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
                                IncidentPriority = metricThreshold.Severity.Value;
                                IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id, metricThreshold.TimePeriod);
                                IncidentPriority = metricThreshold.Severity.Value;
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
                            IncidentPriority = (int)metricThreshold.Severity.Enum;
                            //IncidentMesage = FormatAverageServiceDeskMessage(average, metricTypeDescription, metricThreshold);
                            IncidentMesages.Add(FormatStandardServiceDeskMessage(metricTypeDescription, metricThreshold));
                            IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                            IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
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
                                IncidentPriority = (int)metricThreshold.Severity.Enum;
                                IncidentSummary = FormatSummaryServiceDeskMessage(metricTypeDescription);
                                IncidentMesage = ServiceDeskMessageHeader + Environment.NewLine;
                                IncidentMesages.Add(FormatMatchServiceDeskMessage(metricThreshold));
                                matchFound = true;
                            }
                            else
                            {
                                var count = IncidentService.GetCount(MetricInstance.Id, metricThreshold.Id,
                                                                      metricThreshold.TimePeriod);
                                IncidentPriority = metricThreshold.Severity.Value;
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

        protected override string FormatSummaryServiceDeskMessage(string metricTypeDescription)
        {
            var message = string.Format(ServiceDeskSummary, IncidentPriority, Hostname);
            return message;
        }

        protected override string FormatStandardServiceDeskMessage(string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessage, MetricInstanceId, "", _publisher, _publication, _subscriber, _status, _uploadInserts, _downloadInserts, _uploadUpdates, _downloadUpdates, _uploadDeletes, _downloadDeletes, _lastAction, metricThreshold.Id, metricThreshold.MatchValue, Hostname, ServerId, IpAddress, _instanceName);
            return message;
        }

        protected override string FormatMatchServiceDeskMessage(MetricThreshold metricThreshold)
        {
            //"The Delta monitoring application has detected aTrans Replication breach. (metricInstanceId: {0}).\n\nPublisher: {2}\nPublication: {3}\nSubscriber: {4}\nReplication Exception: {5}\nInsert uploads/downloads: {6}\\{7}\nUpdate uploads/downloads: {8}\\{9}\nDelete uploads/downloads: {10}\\{11}\nLast Action: {12}\n\nMetric Threshold: {13}\nMatch Value: {14}\nServer: {15} ({16})\nIp Address: {17}\n";

            var message = string.Format(ServiceDeskMessage, MetricInstanceId, "", _publisher, _publication, _subscriber, _status, _uploadInserts, _downloadInserts, _uploadUpdates, _downloadUpdates, _uploadDeletes, _downloadDeletes, _lastAction, metricThreshold.Id, metricThreshold.MatchValue, Hostname, ServerId, IpAddress, _instanceName);
            return message;
        }

        protected override string FormatCountServiceDeskMessage(int count, string metricTypeDescription, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageCount, MetricInstanceId, "", _publisher, _publication, _subscriber, _status, _uploadInserts, _downloadInserts, _uploadUpdates, _downloadUpdates, _uploadDeletes, _downloadDeletes, _lastAction, metricThreshold.Id, metricThreshold.MatchValue, Hostname, ServerId, IpAddress, count, metricThreshold.TimePeriod, _instanceName);
            return message;
        }

        protected override string FormatMatchCountServiceDeskMessage(int count, MetricThreshold metricThreshold)
        {
            var message = string.Format(ServiceDeskMessageCount, MetricInstanceId, "", _publisher, _publication, _subscriber, _status, _uploadInserts, _downloadInserts, _uploadUpdates, _downloadUpdates, _uploadDeletes, _downloadDeletes, _lastAction, metricThreshold.Id, metricThreshold.MatchValue, Hostname, ServerId, IpAddress, count, metricThreshold.TimePeriod, _instanceName);
            return message;
        }


        protected override void ParseDataCollection(XDocument dataResultCollection)
        {

            foreach (XElement dataCollection in dataResultCollection.Element("DatabaseServerTransactionalReplicationPluginOutput").Elements("TransactionalReplicationStatus"))
            {
                NameValueCollection collection = new NameValueCollection();

                var xInstanceName = dataCollection.Attribute("instanceName");
                if (xInstanceName != null)
                {
                    _instanceName = xInstanceName.Value;
                    collection.Add("InstanceName", _instanceName);
                }

                var xPublisher = dataCollection.Attribute("publisher");
                if (xPublisher != null)
                {
                    _publisher = xPublisher.Value;
                    collection.Add("Publisher", _publisher);
                }

                var xSubscriber = dataCollection.Attribute("subscriber");
                if (xSubscriber != null)
                {
                    _subscriber = xSubscriber.Value;
                    collection.Add("Subscriber", _subscriber);
                }

                var xPublication = dataCollection.Attribute("publication");
                if (xPublication != null)
                {
                    _publication = xPublication.Value;
                    collection.Add("Publication", _publication);
                }

                var xStatus = dataCollection.Attribute("status");
                if (xStatus != null)
                {
                    _status = long.Parse(xStatus.Value);
                    collection.Add("Status", _status.ToString());
                }

                var xSubscriberDb = dataCollection.Attribute("subscriberDb");
                if (xSubscriberDb != null)
                {
                    _subscriberDb = xSubscriberDb.Value;
                    collection.Add("SubscriberDB", _subscriberDb);
                }

                var xType = dataCollection.Attribute("type");
                if (xType != null)
                {
                    _type = xType.Value;
                    collection.Add("Type", _type);
                }

                var xAgentName = dataCollection.Attribute("agentName");
                if (xAgentName != null)
                {
                    _agentName = xAgentName.Value;
                    collection.Add("AgentName", _agentName);
                }

                var xLastAction = dataCollection.Attribute("lastAction");
                if (xLastAction != null)
                {
                    _lastAction = xLastAction.Value;
                    collection.Add("LastAction", _lastAction);
                }

                var xStartTime = dataCollection.Attribute("startTime");
                if (xStartTime != null)
                {
                    _startTime = xStartTime.Value;
                    collection.Add("StartTime", _startTime);
                }

                var xActionTime = dataCollection.Attribute("actionTime");
                if (xActionTime != null)
                {
                    _actionTime = xActionTime.Value;
                    collection.Add("ActionTime", _actionTime);
                }

                var xDuration = dataCollection.Attribute("duration");
                if (xDuration != null)
                {
                    _duration = xDuration.Value;
                    collection.Add("Duration", _duration);
                }

                var xDeliveryRate = dataCollection.Attribute("deliveryRate");
                if (xDeliveryRate != null)
                {
                    _deliveryRate = xDeliveryRate.Value;
                    collection.Add("DeliveryRate", _deliveryRate);
                }

                var XDownloadInsert = dataCollection.Attribute("downloadInserts");
                if (XDownloadInsert != null)
                {
                    _downloadInserts = XDownloadInsert.Value;
                    collection.Add("DownloadInserts", _downloadInserts);
                }

                var XDownloadUpdates = dataCollection.Attribute("downloadUpdates");
                if (XDownloadUpdates != null)
                {
                    _downloadUpdates = XDownloadUpdates.Value;
                    collection.Add("DownloadUpdates", _downloadUpdates);
                }

                var XDownloadDeletes = dataCollection.Attribute("downloadDeletes");
                if (XDownloadDeletes != null)
                {
                    _downloadDeletes = XDownloadDeletes.Value;
                    collection.Add("DownloadDeletes", _downloadDeletes);
                }

                var XDublisherConflicts = dataCollection.Attribute("publisherConflicts");
                if (XDublisherConflicts != null)
                {
                    _publisherConflicts = XDublisherConflicts.Value;
                    collection.Add("PublisherConflicts", _publisherConflicts);
                }

                var XUploadInserts = dataCollection.Attribute("uploadInserts");
                if (XUploadInserts != null)
                {
                    _uploadInserts = XUploadInserts.Value;
                    collection.Add("UploadInserts", _uploadInserts);
                }

                var XUploadUpdates = dataCollection.Attribute("uploadUpdates");
                if (XUploadUpdates != null)
                {
                    _uploadUpdates = XUploadUpdates.Value;
                    collection.Add("UploadUpdates", _uploadUpdates);
                }

                var XUploadDeletes = dataCollection.Attribute("uploadDeletes");
                if (XUploadDeletes != null)
                {
                    _uploadDeletes = XUploadDeletes.Value;
                    collection.Add("UploadDeletes", _uploadDeletes);
                }

                var XSubscriberConflicts = dataCollection.Attribute("subscriberConflicts");
                if (XSubscriberConflicts != null)
                {
                    _subscriberConflicts = XSubscriberConflicts.Value;
                    collection.Add("SubscriberConflicts", _subscriberConflicts);
                }

                var XErrorId = dataCollection.Attribute("errorId");
                if (XErrorId != null)
                {
                    _errorId = XErrorId.Value;
                    collection.Add("ErrorId", _errorId);
                }

                var XJobId = dataCollection.Attribute("jobId");
                if (XJobId != null)
                {
                    _jobId = XJobId.Value;
                    collection.Add("JobId", _jobId);
                }

                var XLocalJob = dataCollection.Attribute("localJob");
                if (XLocalJob != null)
                {
                    _localJob = XLocalJob.Value;
                    collection.Add("LocalJob", _localJob);
                }

                var XProfileId = dataCollection.Attribute("profileId");
                if (XProfileId != null)
                {
                    _profileId = XProfileId.Value;
                    collection.Add("ProfileId", _profileId);
                }

                var XAgentId = dataCollection.Attribute("agentId");
                if (XAgentId != null)
                {
                    _agentId = XAgentId.Value;
                    collection.Add("AgentId", _agentId);
                }

                var XOffloadEnabled = dataCollection.Attribute("offloadEnabled");
                if (XOffloadEnabled != null)
                {
                    _offloadEnabled = XOffloadEnabled.Value;
                    collection.Add("OffloadEnabled", _offloadEnabled);
                }

                var XOffloadServer = dataCollection.Attribute("offloadServer");
                if (XOffloadServer != null)
                {
                    _offloadServer = XOffloadServer.Value;
                    collection.Add("OffloadServer", _offloadServer);
                }

                var XSubscriberType = dataCollection.Attribute("subscriberType");
                if (XSubscriberType != null)
                {
                    _subscriberType = XSubscriberType.Value;
                    collection.Add("SubscriberType", _subscriberType);
                }

                nodes.Add(collection);

            }

        }
    }
}