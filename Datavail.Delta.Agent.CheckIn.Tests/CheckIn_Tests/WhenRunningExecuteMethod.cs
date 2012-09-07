using System;
using System.Text.RegularExpressions;
using Datavail.Delta.Agent.Plugin.CheckIn;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.CheckIn.Tests.CheckIn_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethod
    {

        [TestMethod]
        public void ThenCheckInIsExecuted()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            var mockServerId = new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}");
            var mockTenantId = new Guid("{77A6C656-19E5-445B-9361-DE5992078FA5}");
            const string mockAgentVersion = "1.0.1234";

            common.Setup(c => c.GetServerId()).Returns(mockServerId);
            common.Setup(c => c.GetTenantId()).Returns(mockTenantId);
            common.Setup(c => c.GetAgentVersion()).Returns(mockAgentVersion);

            var serverId = Guid.Empty;
            var tenantId = Guid.Empty;
            var hostname = string.Empty;
            var ipaddress = string.Empty;
            var agentversion = string.Empty;
            
            var checkIn = new Mock<ICheckInService>();
            checkIn.Setup(c =>
                c.CheckIn(It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>()))
                .Callback<Guid, Guid, string, string, string, Guid?>((tenant, server, host, ip, agent, customer) =>
                    {
                        serverId = server;
                        tenantId = tenant;
                        hostname = host;
                        ipaddress = ip;
                        agentversion = agent;
                    });

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CheckInPlugin(common.Object, checkIn.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            checkIn.Verify(m => m.CheckIn(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once());
            Assert.AreEqual(mockServerId, serverId);
            Assert.AreEqual(mockTenantId, tenantId);
            Assert.AreEqual(mockAgentVersion, agentversion);
            Assert.AreNotEqual(String.Empty, hostname);
            Assert.AreNotEqual(String.Empty, ipaddress);
            Assert.AreNotEqual(String.Empty, agentversion);
        }

        [TestMethod]
        public void ThenLogEntryIsWrittenWhenExceptionIsThrown()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            var mockServerId = Guid.Empty;
            var mockTenantId = Guid.Empty;
            const string mockAgentVersion = "1.0.1234";

            common.Setup(c => c.GetServerId()).Returns(mockServerId);
            common.Setup(c => c.GetTenantId()).Returns(mockTenantId);
            common.Setup(c => c.GetAgentVersion()).Returns(mockAgentVersion);

            var serverId = Guid.Empty;
            var tenantId = Guid.Empty;
            var hostname = string.Empty;
            var ipaddress = string.Empty;
            var agentversion = string.Empty;
            
            var checkIn = new Mock<ICheckInService>();
            checkIn.Setup(c =>
                c.CheckIn(It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>()))
                .Callback<Guid, Guid, string, string, string, Guid?>((tenant, server, host, ip, agent, customer) =>
                {
                    serverId = server;
                    tenantId = tenant;
                    hostname = host;
                    ipaddress = ip;
                    agentversion = agent;
                });

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CheckInPlugin(null, checkIn.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
