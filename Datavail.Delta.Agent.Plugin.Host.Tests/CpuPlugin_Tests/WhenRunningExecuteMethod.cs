using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.RegularExpressions;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.CpuPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethod
    {

        [TestMethod]
        public void ThenDataIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var systemInfo = new Mock<ISystemInfo>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CpuPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }

        [TestMethod]
        public void ThenTimestampIsPopulatedInUploadedData()
        {
            //Arrange
            const string timestampRegex = "timestamp=\\\"\\d{4}\\-\\d{2}\\-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{1,10}Z\"";

            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1073741824L;
            var availableBytes = 25L * 1073741824L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CpuPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(Regex.IsMatch(uploadedData, timestampRegex));
        }

        [TestMethod]
        public void ThenMetricInstanceIdIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1073741824L;
            var availableBytes = 25L * 1073741824L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CpuPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("metricInstanceId=\"" + metricInstanceId + "\""));
        }

        [TestMethod]
        public void ThenLabelIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1073741824L;
            var availableBytes = 25L * 1073741824L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new CpuPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("label=\"C:\\"));
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
