﻿using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.HostPlugin_Tests.DiskInfoThresholdRule_Tests
{
    [TestClass]
    public class WhenMatchingWithMultipleCountPercentageThreshold
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
        private const double TotalBytes = 1073741824;
        private const string TotalBytesFriendly = "1GB";
        private const double AvailableBytes = 536870912;
        private const string AvailableBytesFriendly = "512MB";
        private const float PercentageAvailable = 50;

        //Metric Threshold
        private Guid _metricThresholdId = new Guid("fa4846a3-ed6a-4e16-a556-d33b881f0d11");
        private Severity Severity = Severity.Informational;
        private ThresholdValueType ThresholdValueType = ThresholdValueType.Percentage;
        private ThresholdComparisonFunction ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
        private const float Floor = 50;
        private const float Ceiling = 100;
        private const int MatchCount = 2;
        private const int TimePeriod = 5;

        private MetricInstance _metricInstance;
        private MetricThreshold _metricThreshold;

        private XDocument GetMatchingXml()
        {
            var xml = new XElement("DiskPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", MetricInstanceId),
                                   new XAttribute("label", "some label"),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("totalBytes", TotalBytes),
                                   new XAttribute("totalBytesFriendly", TotalBytesFriendly),
                                   new XAttribute("availableBytes", AvailableBytes),
                                   new XAttribute("availableBytesFriendly", AvailableBytesFriendly),
                                   new XAttribute("percentageAvailable", PercentageAvailable));

            return new XDocument(xml);
        }

        private IServerService SetupServerService()
        {
            var serverService = new Mock<IServerService>();

            _metricInstance = MetricInstance.NewMetricInstance("some label", Metric.NewMetric("assembly", "class", "adapterVersion", "Name"));

            var serverInfoXml = new XElement("ServerInfo", new XAttribute("serverId", ServerId), new XAttribute("hostName", Hostname), new XAttribute("ipAddress", IpAddress));
            serverService.Setup(s => s.GetServerInfoFromMetricInstanceId(It.IsAny<Guid>())).Returns(serverInfoXml.ToString);
            serverService.Setup(s => s.GetMetricInstance(It.IsAny<Guid>())).Returns(_metricInstance);
            
            _metricThreshold = MetricThreshold.NewMetricThreshold(Ceiling, Floor, "", MatchCount, Severity, ThresholdComparisonFunction, ThresholdValueType, TimePeriod, _metricThresholdId);
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

            var rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService);

            Assert.AreEqual(false, rule.IsMatch());
        }

        [TestMethod]
        public void ThenItIsAMatchOnSecondOccurrence()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(1);

            var serverService = SetupServerService();
            var xml = GetMatchingXml();

            var rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(false, rule.IsMatch());

            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(2);
            rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService);
            Assert.AreEqual(true, rule.IsMatch());
        }

        [TestMethod]
        public void ThenAThresholdHistoryRecordIsLogged()
        {
            
            var incidentService = new Mock<IIncidentService>();
            incidentService.Setup(s => s.GetCount(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).Returns(MatchCount);
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService);

            var match = rule.IsMatch();

            incidentService.Verify(s => s.AddMetricThresholdHistory(It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<float>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()));
        }
        
        [TestMethod]
        public void ThenServiceDeskSummaryIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService);
            var expectedMessage = string.Format("P{0}/{1}/Disk {2} threshold breach for {3}", (int)Severity, Hostname, MatchType, Label);

            var match = rule.IsMatch();

            Assert.AreEqual(expectedMessage, rule.IncidentSummary);
        }

        [TestMethod]
        public void ThenServiceDeskPriorityIsCorrect()
        {
            
            var incidentService = new Mock<IIncidentService>();
            var serverService = SetupServerService();
            var xml = GetMatchingXml();
            var rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService);
            var expectedPriority = (int)Severity;

            var match = rule.IsMatch();

            Assert.AreEqual(expectedPriority, rule.IncidentPriority);
        }
    }
}
