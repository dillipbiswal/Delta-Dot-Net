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
    public class WhenMatchingWithSingleCountMatchThreshold
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

        private const string Status2 = "6";
        private const string Publisher2 = "Test Publisher 2";
        private const string Publication2 = "Test Publication 2";
        private const string Subscriber2 = "Test Subscriber 2";
        private const string LastAction2 = "Test Last Action 2";
        private const string DownloadInserts2 = "500";
        private const string DownloadUpdates2 = "500";
        private const string DownloadDeletes2 = "500";
        private const string PublisherConflicts2 = "250";
        private const string UploadInserts2 = "300";
        private const string UploadUpdates2 = "300";
        private const string UploadDeletes2 = "300";
        private const string SubscriberConflicts2 = "150";
        private const string InstanceName2 = "Test Instance 2";
        private const string SubscriberDb2 = "Test Subscriber DB 2";
        private const string Type2 = "Merge";
        private const string AgentName2 = "Test Merge Agent 2";
        private const string StartTime2 = "05:40";
        private const string ActionTime2 = "05:40";
        private const string Duration2 = "400";
        private const string DeliveryRate2 = "35";
        private const string ErrorId2 = "1";
        private const string JobId2 = "2";
        private const string LocalJob2 = "Local Merge Replication Job 2";
        private const string ProfileId2 = "5";
        private const string AgentId2 = "7";
        private const string OffloadEnabled2 = "1";
        private const string OffloadServer2 = "Test Offload Server 2";


        //Metric Threshold
        private Guid _metricThresholdId = new Guid("fa4846a3-ed6a-4e16-a556-d33b881f0d11");
        private Severity Severity = Severity.Informational;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Value;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
        private const float Floor = 5;
        private const float Ceiling = 6;
        private const int MatchCount = 1;
        private const int TimePeriod = -1;
        
        private MetricThreshold _metricThreshold;

        private XDocument GetSingleFaultMatchingXml()
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

        private XDocument GetMultipleMatchingXml()
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


            var xelement2 = new XElement("MergeReplicationStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("publisher", "Test Publisher 2"),
                                   new XAttribute("subscriber", "Test Subscriber 2"),
                                   new XAttribute("publication", "Test Publication 2"),
                                   new XAttribute("status", "6"),
                                   new XAttribute("subscriberDb", "Test Subscriber DB 2"),
                                   new XAttribute("type", "Merge"),
                                   new XAttribute("agentName", "Test Merge Agent 2"),
                                   new XAttribute("lastAction", "Test Last Action 2"),
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
                                    new XAttribute("localJob", "Local Merge Replication Job 2"),
                                    new XAttribute("profileId", "5"),
                                    new XAttribute("agentId", "7"),
                                    new XAttribute("lastTimestamp", "05/05/2022"),
                                    new XAttribute("offloadEnabled", "1"),
                                    new XAttribute("offloadServer", "Test Offload Server 2"),
                                    new XAttribute("subscriberType", "Merge"));

            xmlMain.Root.Add(xelement);
            xmlMain.Root.Add(xelement2);

            return xmlMain;
        }

        private IServerService SetupServerService()
        {
            var serverService = new Mock<IServerService>();
            var metric = Metric.NewMetric("TestAssembly", "TestClass", "TestAdapter", "TestName");
            var metricInstance = MetricInstance.NewMetricInstance("Test Metric Instance", metric);
            serverService.Setup(s => s.GetMetricInstance(It.IsAny<Guid>())).Returns(metricInstance);
            var serverInfoXml = new XElement("ServerInfo", new XAttribute("serverId", ServerId), new XAttribute("hostName", Hostname), new XAttribute("ipAddress", IpAddress));
            serverService.Setup(s => s.GetServerInfoFromMetricInstanceId(It.IsAny<Guid>())).Returns(serverInfoXml.ToString);

            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "6", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
            serverService.Setup(s => s.GetThresholds(It.IsAny<Guid>())).Returns(new[] { _metricThreshold });

            return serverService.Object;
        }
        #endregion
    }
}
