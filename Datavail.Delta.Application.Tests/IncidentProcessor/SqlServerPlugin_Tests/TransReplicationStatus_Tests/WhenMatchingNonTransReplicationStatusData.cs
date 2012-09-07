using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.TransReplicationStatus_Tests
{
    [TestClass]
    public class WhenMatchingNonTransReplicationStatusData
    {
        private XDocument GetNonMatchingXml()
        {
            var xml = new XElement("DatabaseServerTransReplicationPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", "{3572DEF9-9EE9-4EF8-B907-CB69DE6EB13E}"),
                                   new XAttribute("label", "some label"),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("subscriber", "Test Subscriber"),
                                   new XAttribute("publication", "Test Publication"),
                                   new XAttribute("status", "failed"),
                                   new XAttribute("subscriberDb", "Test Subscriber Db"),
                                   new XAttribute("type", "Merge"),
                                   new XAttribute("distributionAgent", "Test Agent"),
                                   new XAttribute("lastAction", ""),
                                   new XAttribute("startTime", "05:40"),
                                   new XAttribute("actionTime", "05:40"),
                                   new XAttribute("duration", "400"),
                                   new XAttribute("deliveryLatency", "10"),
                                   new XAttribute("deliveryTransactions", "1000"),
                                   new XAttribute("deliveredCommands", "1000"),
                                   new XAttribute("averageCommande", "100"),
                                   new XAttribute("errorId", "1"),
                                   new XAttribute("jobId", "2"),
                                   new XAttribute("localJob", "Local Trans Job"),
                                   new XAttribute("profileId", "454"),
                                   new XAttribute("agentId", "32"),
                                   new XAttribute("offloadEnabled", "1"),
                                   new XAttribute("offloadServer", "Test Offload Server"),
                                   new XAttribute("subscriberType", "Transactinal"));



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

            var rule = new MergeReplicationStatusRule(incidentService.Object, xml, serverService.Object);

            Assert.AreEqual(false, rule.IsMatch());
        }
    }
}
