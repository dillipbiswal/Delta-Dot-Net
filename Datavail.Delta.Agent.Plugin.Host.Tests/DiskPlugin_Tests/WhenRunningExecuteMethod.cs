using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.RegularExpressions;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.DiskPlugin_Tests
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

            var totalBytes = 50L * 1073741824L;
            var availableBytes = 25L * 1073741824L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "/", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }
        //

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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("label=\"C:\\"));
        }

        [TestMethod]
        public void ThenTotalBytesIsPopulatedInUploadedData()
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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalBytes=\"" + totalBytes + "\""));
        }

        [TestMethod]
        public void ThenTotalBytesFriendlyIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1073741824L;
            var availableBytes = 25L * 1073741824L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var dataUploader = new Mock<IDataQueuer>();
            var uploadedData = String.Empty;
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalBytesFriendly=\"50 GB\""));
        }

        [TestMethod]
        public void ThenAvailableBytesTbIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1099511627776L;
            var availableBytes = 25L * 1099511627776L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableBytesFriendly=\"25 TB\""));
        }

        [TestMethod]
        public void ThenAvailableBytesGbIsPopulatedInUploadedData()
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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableBytesFriendly=\"25 GB\""));
        }

        [TestMethod]
        public void ThenAvailableBytesMbIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1048576L;
            var availableBytes = 25L * 1048576L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableBytesFriendly=\"25 MB\""));
        }

        [TestMethod]
        public void ThenAvailableBytesFriendlyIsPopulatedTbInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1099511627776L;
            var availableBytes = 25L * 1099511627776L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableBytesFriendly=\"25 TB\""));
        }

        [TestMethod]
        public void ThenAvailableBytesFriendlyIsPopulatedGbInUploadedData()
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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableBytesFriendly=\"25 GB\""));
        }

        [TestMethod]
        public void ThenAvailableBytesFriendlyIsPopulatedMbInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = 50L * 1048576L;
            var availableBytes = 25L * 1048576L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableBytesFriendly=\"25 MB\""));
        }

        [TestMethod]
        public void ThenPercentageAvailableIsPopulatedInUploadedData()
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
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            Assert.IsTrue(uploadedData.Contains("percentageAvailable=\"50\""));
        }

        [TestMethod]
        public void ThenLogEntryIsWrittenWhenExceptionIsThrown()
        {
            //Arrange
            var metricInstanceId = Guid.Empty;

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var totalBytes = -1L;// 50L * 1073741824L;
            var availableBytes = 0L;// 25L * 1073741824L;
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetDiskFreeSpace(It.IsAny<string>(), out totalBytes, out availableBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();
            var clusterInfo = new Mock<IClusterInfo>();

            var plugin = new DiskPlugin(systemInfo.Object, dataUploader.Object, logger.Object, clusterInfo.Object);

            //Act
            plugin.Execute(metricInstanceId, "C:\\", "<DiskPluginInput Path=\"C:\\\" />");

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
