using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.LongRunningProcess_Tests
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
        private const string LongRunningThreshold = "60";
        private const string SqlStatement = "select foo from bar";
        private const string Spid = "123";
        private const string ProgramName = "Test Program Name";
        private const string CurrentRunTime = "120";
        private const string LastBatch = "20";
        private const string InstanceName = "Test Instance";

        //Metric Threshold
        private Guid _metricThresholdId = new Guid("fa4846a3-ed6a-4e16-a556-d33b881f0d11");
        private Severity Severity = Severity.Informational;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Value;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
        private const float Floor = 50;
        private const float Ceiling = 100;
        private const int MatchCount = 2;
        private const int TimePeriod = 5;

        private MetricInstance _metricInstance;
        private MetricThreshold _metricThreshold;

        private XDocument GetSingleFaultMatchingXml()
        {
            var xmlMain = new XDocument(
                            new XElement("DatabaseServerLongRunningProcessPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", MetricInstanceId),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("label", "some label")));


            var xelement = new XElement("LongRunningProcessResult",
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("longProcessThreshold", LongRunningThreshold),
                                   new XAttribute("currentRunTime", CurrentRunTime),
                                   new XAttribute("spid", Spid),
                                   new XAttribute("programName", ProgramName),
                                   new XAttribute("lastBatch", LastBatch),
                                   new XAttribute("sqlStatements", SqlStatement));


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
            
            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "60", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
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

            var rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);

            Assert.AreEqual(false, rule.IsMatch());
        }

        [TestMethod]
        public void ThenItIsAMatchOnSecondOccurrence()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(1);

            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();

            var rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(false, rule.IsMatch());

            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(2);
            rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(true, rule.IsMatch());
        }

        [TestMethod]
        public void ThenAThresholdHistoryRecordIsLogged()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);

            var match = rule.IsMatch();

            incidentService.Verify(s=>s.AddMetricThresholdHistory(It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [TestMethod]
        public void ThenServiceDeskMessageIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("The Delta monitoring application has detected the following Long Running Process(s).\r\n(metricInstanceId: {0}). This has occurred {1} times in the last {2} minutes.\n\nInstance Name: {3}\nProgram Name: {4}\nSQL Statement: {5}\nSPID: {6}\n\nMatch Value: {7}\nMetric Threshold: {8}\nServer: {9}\nIp Address: {10}\n----------------------------------------------------------------------\r\n\r\n", MetricInstanceId, MatchCount, _metricThreshold.TimePeriod, InstanceName, ProgramName, SqlStatement, Spid, _metricThreshold.MatchValue, _metricThreshold.Id, Hostname, IpAddress);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentMesage);
        }

        [TestMethod]
        public void ThenServiceDeskSummaryIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("P{0}/{1}/Long Running Process(s) detected.", (int)Severity, Hostname);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentSummary);
        }

        [TestMethod]
        public void ThenServiceDeskPriorityIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetSingleFaultMatchingXml();
            var rule = new LongRunningProcessRule(incidentService.Object, xml, serverService);
            var expectedPriority = (int)Severity;

            var match = rule.IsMatch();

            Assert.AreEqual(expectedPriority, rule.IncidentPriority);
        }
    }
}
