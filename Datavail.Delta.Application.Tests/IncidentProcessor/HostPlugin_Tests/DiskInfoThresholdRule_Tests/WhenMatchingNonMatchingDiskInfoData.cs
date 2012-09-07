using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.HostPlugin;
using Datavail.Delta.Application.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.HostPlugin_Tests.DiskInfoThresholdRule_Tests
{
    [TestClass]
    public class WhenMatchingNonMatchingDiskInfoData
    {
        private XDocument GetNonMatchingXml()
        {
            var xml = new XElement("DiskPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", "{3572DEF9-9EE9-4EF8-B907-CB69DE6EB13E}"),
                                   new XAttribute("label", "some label"),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("totalBytes", 1073741824),
                                   new XAttribute("totalBytesFriendly", "1GB"),
                                   new XAttribute("availableBytes", 536870912),
                                   new XAttribute("availableBytesFriendly", "512MB"),
                                   new XAttribute("percentageAvailable", 50));

            return new XDocument(xml);
        }

        [TestMethod]
        public void ThenItIsNotAMatch()
        {
            
            var incidentService = new Mock<IIncidentService>();

            var serverService = new Mock<IServerService>();
            var serverInfoXml = new XElement("ServerInfo", new XAttribute("serverId", "B7A7EC5F-5EA7-497C-8393-43EA1D47214A"), new XAttribute("hostName", "testhost"), new XAttribute("ipAddress", "10.1.1.1"));
            serverService.Setup(s => s.GetServerInfoFromMetricInstanceId(It.IsAny<Guid>())).Returns(serverInfoXml.ToString);

            var xml = GetNonMatchingXml();

            var rule = new DiskInfoThresholdRule(incidentService.Object, xml, serverService.Object);

            Assert.AreEqual(false, rule.IsMatch());
        }
    }
}
