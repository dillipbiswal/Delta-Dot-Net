using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.CpuPlugin_Tests
{
    [TestClass]
    public class WhenRunningGetLogicalDrivesMethod
    {

        [TestMethod]
        public void ThenValueAsFloatIsReturned()
        {
            //Arrange
            SystemInfo systemInfo = new SystemInfo();


            //Act
            var returnValue = systemInfo.GetCpuUtilization();

            //Assert
            Assert.IsInstanceOfType(returnValue, typeof(float));
        }

        [TestMethod]
        public void ThenLogEntryIsWrittenWhenExceptionIsThrown()
        {
            //Arrange
            var metricInstanceId = Guid.Empty;

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CpuPlugin(null, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
