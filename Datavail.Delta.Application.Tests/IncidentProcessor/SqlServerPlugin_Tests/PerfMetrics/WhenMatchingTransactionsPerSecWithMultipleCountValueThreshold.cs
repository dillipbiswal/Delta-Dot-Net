using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.PerfMetrics
{
    [TestClass]
    public class WhenMatchingTransactionsPerSecWithMultipleCountValueThreshold
    {
        #region Variables and Helpers
        //Common
        private const string MatchType = "Transactions/Sec";
        private const string MetricInstanceId = "3572def9-9ee9-4ef8-b907-cb69de6eb13e";
        private const string Label = "(minutes since last backup.)";
        
        //ServerInfo
        private const string ServerId = "b7a7ec5f-5ea7-497c-8393-43ea1d47214a";
        private const string Hostname = "testhost";
        private const string IpAddress = "10.1.1.1";

        //DataCollection Specific
        private const long TransactionsPerSec = 600000;
        private const string InstanceName = "Test Instance";


        //Metric Threshold
        private Guid _metricThresholdId = new Guid("3572def9-9ee9-4ef8-b907-cb69de6eb13e");
        private Severity Severity = Severity.Critical;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Value;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
        private const float Floor = 500;
        private const float Ceiling = 1000001;
        private const int MatchCount = 2;
        private const int TimePeriod = 5;

        private MetricInstance _metricInstance;
        private MetricThreshold _metricThreshold;

        private XDocument GetMatchingXml()
        {
            var xml = new XElement("DatabaseServerPerformanceCountersPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", "{3572def9-9ee9-4ef8-b907-cb69de6eb13e}"),
                                   new XAttribute("label", "Test Database"),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("batchRequestsPerSec", "1000000"),
                                   new XAttribute("checkpointPagesPerSec", "2000000"),
                                   new XAttribute("lazyWritesPerSec", "3000000"),
                                   new XAttribute("logFlushesPerSec", "4000000"),
                                   new XAttribute("pageLifeExpectancy", "60"),
                                   new XAttribute("pageLookupsPerSec", "100000"),
                                   new XAttribute("pageSplitsPerSec", "200000"),
                                   new XAttribute("sqlCompilationsPerSec", "300000"),
                                   new XAttribute("targetServerMemoryKb", "400000"),
                                   new XAttribute("totalServerMemoryKb", "500000"),
                                   new XAttribute("transactionsPerSec", "600000"));

            return new XDocument(xml);
        }

        private IServerService SetupServerService()
        {
            var serverService = new Mock<IServerService>();
            _metricInstance = MetricInstance.NewMetricInstance("some label", Metric.NewMetric("assembly", "class", "adapterVersion", "Name"));

            var serverInfoXml = new XElement("ServerInfo", new XAttribute("serverId", ServerId), new XAttribute("hostName", Hostname), new XAttribute("ipAddress", IpAddress));
            serverService.Setup(s => s.GetServerInfoFromMetricInstanceId(It.IsAny<Guid>())).Returns(serverInfoXml.ToString);
            serverService.Setup(s => s.GetMetricInstance(It.IsAny<Guid>())).Returns(_metricInstance);

            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "600000", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
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

            var rule = new PerfMetricsBatchRequestsPerSecRule(incidentService.Object, xml, serverService);

            Assert.AreEqual(false, rule.IsMatch());
        }

        [TestMethod]
        public void ThenItIsAMatchOnSecondOccurrence()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(1);

            var serverService = SetupServerService();
            var xml = GetMatchingXml();

            var rule = new PerfMetricsTransactionsPerSecRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(false, rule.IsMatch());

            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(2);
            rule = new PerfMetricsTransactionsPerSecRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(true, rule.IsMatch());
        }

        [TestMethod]
        public void ThenAThresholdHistoryRecordIsLogged()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new PerfMetricsTransactionsPerSecRule(incidentService.Object, xml, serverService);

            var match = rule.IsMatch();

            incidentService.Verify(s=>s.AddMetricThresholdHistory(It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [TestMethod]
        public void ThenServiceDeskMessageIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new PerfMetricsTransactionsPerSecRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("The Delta monitoring application has detected a performance metrics threshold breach for {0} (metricInstanceId: {1}).This has occurred {2} times in the last {3} minutes.\n\nInstance Name: {11}\nTransactions per Second: {4} \n\nMetric Threshold: {5}\nFloor Value: {6}\nCeiling Value: {7}\nServer: {8} ({9})\nIp Address: {10}\n", MatchType, MetricInstanceId, MatchCount, _metricThreshold.TimePeriod, TransactionsPerSec, _metricThreshold.Id, _metricThreshold.FloorValue, _metricThreshold.CeilingValue, Hostname, ServerId, IpAddress, InstanceName);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentMesage);
        }

        [TestMethod]
        public void ThenServiceDeskSummaryIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new PerfMetricsTransactionsPerSecRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("P{0}/{1}/Performance Metrics ({2}) threshold breach", (int)Severity, Hostname, MatchType);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentSummary);
        }

        [TestMethod]
        public void ThenServiceDeskPriorityIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new PerfMetricsTransactionsPerSecRule(incidentService.Object, xml, serverService);
            var expectedPriority = (int) Severity;

            var match = rule.IsMatch();

            Assert.AreEqual(expectedPriority, rule.IncidentPriority);
        }
    }
}
