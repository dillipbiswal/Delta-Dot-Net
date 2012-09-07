using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.DatabaseServerBlocking_Tests
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
        private const string Database = "Test Database";
        private const string RequestSessionId = "1";
        private const string RequestSessionCommand = "Select * from foo";
        private const string WaitingDurationSec = "5";
        private const string BlockingId = "10";
        private const string BlockingCommand = "insert this into that";
        private const string InstanceName = "Test Instance";
        private const string RequestSessionId2 = "2";
        private const string RequestSessionCommand2 = "Select item from items";
        private const string WaitingDurationSec2 = "51";
        private const string BlockingId2 = "100";
        private const string BlockingCommand2 = "delete this from that";



        //Metric Threshold
        private Guid _metricThresholdId = new Guid("fa4846a3-ed6a-4e16-a556-d33b881f0d11");
        private Severity Severity = Severity.Informational;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Value;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
        private const float Floor = -1;
        private const float Ceiling = 100;
        private const int MatchCount = 2;
        private const int TimePeriod = 5;

        private MetricThreshold _metricThreshold;


        private XDocument GetSingleFaultMatchingXml()
        {
            var xmlMain = new XDocument(
                            new XElement("DatabaseServerBlockingPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", MetricInstanceId),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("label", "some label")));


            var xelement = new XElement("BlockingStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("instanceName", InstanceName),
                                   new XAttribute("database", Database),
                                   new XAttribute("requestSessionId", RequestSessionId),
                                   new XAttribute("requestSessionCommand", RequestSessionCommand),
                                   new XAttribute("waitingDurationSec", WaitingDurationSec),
                                   new XAttribute("blockingId", BlockingId),
                                   new XAttribute("blockingCommand", BlockingCommand));


            xmlMain.Root.Add(xelement);

            return xmlMain;
        }

        private XDocument GetTwoFaultMatchingXml()
        {
            var xmlMain = new XDocument(
                            new XElement("DatabaseServerBlockingPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", MetricInstanceId),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("label", "some label")));


            var xelement1 = new XElement("BlockingStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("instanceName", InstanceName),
                                   new XAttribute("database", Database),
                                   new XAttribute("requestSessionId", RequestSessionId),
                                   new XAttribute("requestSessionCommand", RequestSessionCommand),
                                   new XAttribute("waitingDurationSec", WaitingDurationSec),
                                   new XAttribute("blockingId", BlockingId),
                                   new XAttribute("blockingCommand", BlockingCommand));

            var xelement2 = new XElement("BlockingStatus",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("instanceName", InstanceName),
                                   new XAttribute("database", Database),
                                   new XAttribute("requestSessionId", RequestSessionId2),
                                   new XAttribute("requestSessionCommand", RequestSessionCommand2),
                                   new XAttribute("waitingDurationSec", WaitingDurationSec2),
                                   new XAttribute("blockingId", BlockingId2),
                                   new XAttribute("blockingCommand", BlockingCommand2));

            xmlMain.Root.Add(xelement1);
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

            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "2", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
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
            var xml = GetSingleFaultMatchingXml();

            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);

            Assert.AreEqual(false, rule.IsMatch());
        }

        [TestMethod]
        public void ThenItIsAMatchOnSecondOccurrence()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(1);

            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();

            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(false, rule.IsMatch());

            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(2);
            rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(true, rule.IsMatch());
        }

        [TestMethod]
        public void ThenAThresholdHistoryRecordIsLogged()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);

            var match = rule.IsMatch();

            incidentService.Verify(s => s.AddMetricThresholdHistory(It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [TestMethod]
        public void ThenSingleFaultServiceDeskMessageIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);
            //var expectedMessage = string.Format("The Delta monitoring application has detected that the job {0} is reporting a status of {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nInstance Name: {10}\nJob Name: {0}\nJob Status: {1}\n\nMetric Threshold: {5}\nMatch Value: {6}\nServer: {7} ({8})\nIp Address: {9}\n", JobName, JobStatus, MetricInstanceId, MatchCount, _metricThreshold.TimePeriod, JobStatus, _metricThreshold.Id, _metricThreshold.MatchValue, Hostname, IpAddress, InstanceName);
            var expectedMessage =
                string.Format(
                    "The Delta monitoring application has detected the following blocking command(s).\r\n(metricInstanceId: {0}). This has occurred {1} times in the last {2} minutes.\n\nInstance Name: {3}\nBlocking Command: {4}\nBlocking Id: {5} \nRequest Session Command: {6}\nRequest Session Id: {7}\nDatabase: {8}\n\nMatch Value: {9}\nMetric Threshold: {10}\nServer: {11}\nIp Address: {12}\n----------------------------------------------------------------------\r\n\r\n", MetricInstanceId, MatchCount, _metricThreshold.TimePeriod, InstanceName, BlockingCommand, BlockingId, RequestSessionCommand, RequestSessionId, Database, _metricThreshold.MatchValue, _metricThresholdId, Hostname, IpAddress);
            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentMesage);
        }

        [TestMethod]
        public void ThenTwoFaultServiceDeskMessageIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetTwoFaultMatchingXml();
            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);
            //var expectedMessage = string.Format("The Delta monitoring application has detected that the job {0} is reporting a status of {1} (metricInstanceId: {2}). This has occurred {3} times in the last {4} minutes.\n\nInstance Name: {10}\nJob Name: {0}\nJob Status: {1}\n\nMetric Threshold: {5}\nMatch Value: {6}\nServer: {7} ({8})\nIp Address: {9}\n", JobName, JobStatus, MetricInstanceId, MatchCount, _metricThreshold.TimePeriod, JobStatus, _metricThreshold.Id, _metricThreshold.MatchValue, Hostname, IpAddress, InstanceName);
            var expectedMessage =
                string.Format(
                    "The Delta monitoring application has detected the following blocking command(s).\r\n(metricInstanceId: 3572def9-9ee9-4ef8-b907-cb69de6eb13e). This has occurred 2 times in the last 5 minutes.\n\nInstance Name: Test Instance\nBlocking Command: insert this into that\nBlocking Id: 10 \nRequest Session Command: Select * from foo\nRequest Session Id: 1\nDatabase: Test Database\n\nMatch Value: 2\nMetric Threshold: fa4846a3-ed6a-4e16-a556-d33b881f0d11\nServer: testhost\nIp Address: 10.1.1.1\n----------------------------------------------------------------------\r\n\r\n(metricInstanceId: 3572def9-9ee9-4ef8-b907-cb69de6eb13e). This has occurred 2 times in the last 5 minutes.\n\nInstance Name: Test Instance\nBlocking Command: delete this from that\nBlocking Id: 100 \nRequest Session Command: Select item from items\nRequest Session Id: 2\nDatabase: Test Database\n\nMatch Value: 2\nMetric Threshold: fa4846a3-ed6a-4e16-a556-d33b881f0d11\nServer: testhost\nIp Address: 10.1.1.1\n----------------------------------------------------------------------\r\n\r\n");
            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentMesage);
        }

        [TestMethod]
        public void ThenServiceDeskSummaryIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("P{0}/{1}/Blocking Command(s) detected.", (int)Severity, Hostname);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentSummary);
        }

        [TestMethod]
        public void ThenServiceDeskPriorityIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new DatabaseServerBlockingRule(incidentService.Object, xml, serverService);
            var expectedPriority = (int)Severity;

            var match = rule.IsMatch();

            Assert.AreEqual(expectedPriority, rule.IncidentPriority);
        }
    }
}
