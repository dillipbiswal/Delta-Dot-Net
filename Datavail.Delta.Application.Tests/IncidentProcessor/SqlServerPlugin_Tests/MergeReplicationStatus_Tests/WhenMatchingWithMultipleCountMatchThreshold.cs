using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.MergeReplicationStatus_Tests
{
    [TestClass]
    public class WhenMatchingWithMultipleCountMatchThreshold
    {
        #region Variables and Helpers
        //Common
        private const string MatchType = "percentage available";
        private const string MetricInstanceId = "3572def9-9ee9-4ef8-b907-cb69de6eb13e";
        private const string Label = "some label";
        
        //ServerInfo
        private const string ServerId = "b7a7ec5f-5ea7-497c-8393-43ea1d47214a";
        private const string Hostname = "testhost";
        private const string IpAddress = "10.1.1.1";

        //DataCollection Specific
        private const string Status = "6";
        private const string Publisher = "Test Publisher";
        private const string Publication = "Test Publication";
        private const string Subscriber = "Test Subscriber";
        private const string LastAction = "Test Last Action";
        private const string DownloadInserts = "500";
        private const string DownloadUpdates = "500";
        private const string DownloadDeletes = "500";
        private const string PublisherConflicts = "250";
        private const string UploadInserts = "300";
        private const string UploadUpdates = "300";
        private const string UploadDeletes = "300";
        private const string SubscriberConflicts = "150";
        private const string InstanceName = "Test Instance";
        private const string SubscriberDb = "Test Subscriber DB";
        private const string Type = "Merge";
        private const string AgentName = "Test Merge Agent";
        private const string StartTime = "05:40";
        private const string ActionTime = "05:40";
        private const string Duration = "400";
        private const string DeliveryRate = "35";
        private const string ErrorId = "1";
        private const string JobId = "2";
        private const string LocalJob = "Local Merge Replication Job";
        private const string ProfileId = "5";
        private const string AgentId = "7";
        private const string OffloadEnabled = "1";
        private const string OffloadServer = "Test Offload Server";


        //Metric Threshold
        private Guid _metricThresholdId = new Guid("fa4846a3-ed6a-4e16-a556-d33b881f0d11");
        private Severity Severity = Severity.Informational;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Value;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
        private const float Floor = 5;
        private const float Ceiling = 6;
        private const int MatchCount = 2;
        private const int TimePeriod = 5;

        private MetricInstance _metricInstance;
        private MetricThreshold _metricThreshold;

        private XDocument GetMatchingXml()
        {
            var xmlMain = new XDocument(
                            new XElement("DatabaseServerMergeReplicationPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", MetricInstanceId),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("label", "some label")));


            var xelement = new XElement("MergeReplicationStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("publisher", "Test Publisher"),
                                   new XAttribute("subscriber", "Test Subscriber"),
                                   new XAttribute("publication", "Test Publication"),
                                   new XAttribute("status", "6"),
                                   new XAttribute("subscriberDb", "Test Subscriber DB"),
                                   new XAttribute("type", "Merge"),
                                   new XAttribute("agentName", "Test Merge Agent"),
                                   new XAttribute("lastAction", "Test Last Action"),
                                   new XAttribute("startTime", "05:40"),
                                   new XAttribute("actionTime", "05:40"),
                                   new XAttribute("duration", "400"),
                                   new XAttribute("deliveryRate", "35"),
                                   new XAttribute("downloadInserts", "500"),
                                    new XAttribute("downloadUpdates", "500"),
                                    new XAttribute("downloadDeletes", "500"),
                                    new XAttribute("publisherConflicts", "250"),
                                    new XAttribute("uploadInserts", "300"),
                                    new XAttribute("uploadUpdates", "300"),
                                    new XAttribute("uploadDeletes", "300"),
                                    new XAttribute("subscriberConflicts", "150"),
                                    new XAttribute("errorId", "1"),
                                    new XAttribute("jobId", "2"),
                                    new XAttribute("localJob", "Local Merge Replication Job"),
                                    new XAttribute("profileId", "5"),
                                    new XAttribute("agentId", "7"),
                                    new XAttribute("lastTimestamp", "05/05/2022"),
                                    new XAttribute("offloadEnabled", "1"),
                                    new XAttribute("offloadServer", "Test Offload Server"),
                                    new XAttribute("subscriberType", "Merge"));

            xmlMain.Root.Add(xelement);

            return xmlMain;
        }

        private IServerService SetupServerService()
        {
            var serverService = new Mock<IServerService>();
            
            _metricInstance = MetricInstance.NewMetricInstance("some label", Metric.NewMetric("assembly", "class", "adapterVersion", "Name"));

            var serverInfoXml = new XElement("ServerInfo", new XAttribute("serverId", ServerId), new XAttribute("hostName", Hostname), new XAttribute("ipAddress", IpAddress));
            serverService.Setup(s => s.GetServerInfoFromMetricInstanceId(It.IsAny<Guid>())).Returns(serverInfoXml.ToString);
            serverService.Setup(s => s.GetMetricInstance(It.IsAny<Guid>())).Returns(_metricInstance);
            
            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "6", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
            serverService.Setup(s => s.GetThresholds(It.IsAny<Guid>())).Returns(new[] { _metricThreshold });

            return serverService.Object;
        }
        #endregion

        [TestMethod]
        public void ThenItIsNotAMatchOnFirstOccurrence()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(1);

            var serverService = SetupServerService();
            var xml = GetMatchingXml();

            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);

            Assert.AreEqual(false, rule.IsMatch());
        }

        [TestMethod]
        public void ThenItIsAMatchOnSecondOccurrence()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(1);

            var serverService = SetupServerService();
            var xml = GetMatchingXml();

            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(false, rule.IsMatch());

            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(2);
            rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(true, rule.IsMatch());
        }

        [TestMethod]
        public void ThenAThresholdHistoryRecordIsLogged()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);

            var match = rule.IsMatch();

            incidentService.Verify(s=>s.AddMetricThresholdHistory(It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [TestMethod]
        public void ThenServiceDeskMessageIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);
            
            var match = rule.IsMatch();
            var _timestamp = rule.Timestamp;
            var expectedMessage = string.Format("The Delta monitoring application has detected the following Merge Replication fault(s).\r\n(metricInstanceId: {0}).\n\nThis has occurred {35} times in the last {36} minutes.\n\nInstance Name: {1}\nDistribution Host: {2}\nReplication Status: {3}\nPublisher: {4}\nPublication: {5}\nSubscriber: {6}\nSubscriber DB: {7}\nType: {8}\nAgent Name: {9}\nLast Action: {10}\nStart Time: {11}\nAction Time: {12}\nDuration: {13}\nDelivery Rate: {14}\nPublisher Conflicts: {15}\nSubscriber Conflicts: {16}\nInsert uploads/downloads: {17}\\{18}\nUpdate uploads/downloads: {19}\\{20}\nDelete uploads/downloads: {21}\\{22}\nError ID: {23}\nJod ID: {24}\nLocal Job: {25}\nProfile ID: {26}\nAgent ID: {27}\nOffload Enabled: {28}\nOffload Server: {29}\n\nAgent Timestamp (UTC): {37}\nMetric Threshold: {30}\nMatch Value: {31}\nServer: {32} ({33})\nIp Address: {34}\n----------------------------------------------------------------------\r\n\r\n", MetricInstanceId, InstanceName, Hostname, Status, Publisher, Publication, Subscriber, SubscriberDb, Type, AgentName, LastAction, StartTime, ActionTime, Duration, DeliveryRate, PublisherConflicts, SubscriberConflicts, DownloadInserts, UploadInserts, DownloadUpdates, UploadUpdates, DownloadDeletes, UploadDeletes, ErrorId, JobId, LocalJob, ProfileId, AgentId, OffloadEnabled, OffloadServer, _metricThreshold.Id, _metricThreshold.MatchValue, Hostname, ServerId, IpAddress, MatchCount, _metricThreshold.TimePeriod, _timestamp);

            Assert.AreEqual(expectedMessage, rule.IncidentMesage);
        }

        [TestMethod]
        public void ThenServiceDeskSummaryIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("P{0}/{1}/Merge Replication threshold(s) breached.", (int)Severity, Hostname, "", "");

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentSummary);
        }

        [TestMethod]
        public void ThenServiceDeskPriorityIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService);
            var expectedPriority = (int) Severity;

            var match = rule.IsMatch();

            Assert.AreEqual(expectedPriority, rule.IncidentPriority);
        }
    }
}
