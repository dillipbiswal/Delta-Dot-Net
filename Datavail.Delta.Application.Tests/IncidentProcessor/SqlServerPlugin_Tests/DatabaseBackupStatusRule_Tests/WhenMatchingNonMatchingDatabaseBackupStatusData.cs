using System;
using System.Xml.Linq;
using Datavail.Delta.Application.IncidentProcessor.Rules.SqlServerPlugin;
using Datavail.Delta.Application.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Application.Tests.IncidentProcessor.Rules.SqlServerPlugin_Tests.DatabaseBackupStatusRule_Tests
{
    [TestClass]
    public class WhenMatchingNonMatchingCpuData
    {
        private XDocument GetNonMatchingXml()
        {
            var xml = new XElement("DatabaseBackupStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", "{3572DEF9-9EE9-4EF8-B907-CB69DE6EB13E}"),
                                   new XAttribute("label", "some label"),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("name", "Test Backup"),
                                   new XAttribute("instanceName", "Test Instance"),
                                   new XAttribute("physicalDeviceName", "c:\\backups"),
                                   new XAttribute("finishTime", "0540"),
                                   new XAttribute("backupFinishDate", "5/5/2010"),
                                   new XAttribute("minsSinceLast", "1000"));

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

            var rule = new DatabaseBackupStatusRule(incidentService.Object, xml, serverService.Object);

            Assert.AreEqual(false, rule.IsMatch());
        }
    }
}
