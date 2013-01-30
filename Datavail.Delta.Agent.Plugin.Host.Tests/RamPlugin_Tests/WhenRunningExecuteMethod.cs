using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.RegularExpressions;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.RamPlugin_Tests
{
    
    [TestClass]
    public class WhenRunningExecuteMethod
    {
        #region Helpers
        ulong _totalPhysicalMemoryBytes = 4L * 1073741824L;
        ulong _totalVirtualMemoryBytes = 2L * 1073741824L;
        ulong _availablePhysicalMemoryBytes = 2L * 1073741824L;
        ulong _availableVirtualMemoryBytes = 1L * 1073741824L;
            
        private ICommon SetupMockCommon()
        {
            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));
            
            return common.Object;
        }
        #endregion

        [TestMethod]
        public void ThenDataIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new SystemInfo();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo, dataUploader.Object, logger.Object);

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

            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(Regex.IsMatch(uploadedData, timestampRegex));
        }

        [TestMethod]
        public void ThenMetricInstanceIdIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("metricInstanceId=\"" + metricInstanceId + "\""));
        }

        [TestMethod]
        public void ThenTotalPhysicalMemoryPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalPhysicalMemoryBytes=\"" + _totalPhysicalMemoryBytes + "\""));
        }

        [TestMethod]
        public void ThenTotalPhysicalMemoryFriendlyTbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            _totalPhysicalMemoryBytes = 4L*1099511627776;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalPhysicalMemoryFriendly=\"4 TB\""));
        }

        [TestMethod]
        public void ThenTotalPhysicalMemoryFriendlyGbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalPhysicalMemoryFriendly=\"4 GB\""));
        }

        [TestMethod]
        public void ThenTotalPhysicalMemoryFriendlyMbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            _totalPhysicalMemoryBytes = 4L * 1048576;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalPhysicalMemoryFriendly=\"4 MB\""));
        }

        [TestMethod]
        public void ThenTotalVirutalMemoryPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalVirtualMemoryBytes=\"" + _totalVirtualMemoryBytes + "\""));
        }

        [TestMethod]
        public void ThenTotalVirtualMemoryFriendlyTbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            _totalVirtualMemoryBytes = 2L*1099511627776;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalVirtualMemoryFriendly=\"2 TB\""));
        }

        [TestMethod]
        public void ThenTotalVirtualMemoryFriendlyGbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalVirtualMemoryFriendly=\"2 GB\""));
        }

        [TestMethod]
        public void ThenTotalVirtualMemoryFriendlyMbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            _totalVirtualMemoryBytes = 2L * 1048576;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("totalVirtualMemoryFriendly=\"2 MB\""));
        }

        [TestMethod]
        public void ThenTotalAvailableMemoryPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availablePhysicalMemoryBytes=\"" + _availablePhysicalMemoryBytes + "\""));
        }

        [TestMethod]
        public void ThenAvailablePhysicalMemoryFriendlyTbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            _availablePhysicalMemoryBytes = 2L * 1099511627776;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availablePhysicalMemoryFriendly=\"2 TB\""));
        }

        [TestMethod]
        public void ThenAvailablePhysicalMemoryFriendlyGbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availablePhysicalMemoryFriendly=\"2 GB\""));
        }

        [TestMethod]
        public void ThenAvailablePhysicalMemoryFriendlyMbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();

            var systemInfo = new Mock<ISystemInfo>();
            _availablePhysicalMemoryBytes = 2L * 1048576;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availablePhysicalMemoryFriendly=\"2 MB\""));
        }

        [TestMethod]
        public void ThenAvailableVirutalMemoryPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableVirtualMemoryBytes=\"" + _availableVirtualMemoryBytes + "\""));
        }

        [TestMethod]
        public void ThenAvailableVirtualMemoryFriendlyTbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            _availableVirtualMemoryBytes = 1L * 1099511627776;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableVirtualMemoryFriendly=\"1 TB\""));
        }

        [TestMethod]
        public void ThenAvailableVirtualMemoryFriendlyGbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableVirtualMemoryFriendly=\"1 GB\""));
        }

        [TestMethod]
        public void ThenAvailableVirtualMemoryFriendlyMbPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            _availableVirtualMemoryBytes = 1L * 1048576;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("availableVirtualMemoryFriendly=\"1 MB\""));
        }

        [TestMethod]
        public void ThenPercentageAvailablePhysicalMemoryPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("percentagePhysicalMemoryAvailable=\"50\""));
        }

        [TestMethod]
        public void ThenPercentageAvailableVirtualMemoryPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            Assert.IsTrue(uploadedData.Contains("percentageVirtualMemoryAvailable=\"50\""));
        }

        [TestMethod]
        public void ThenLogEntryIsWrittenWhenExceptionIsThrown()
        {
            //Arrange
            var metricInstanceId = Guid.Empty;

            var common = SetupMockCommon();
            var systemInfo = new Mock<ISystemInfo>();

            _totalPhysicalMemoryBytes = 0UL;
            systemInfo.Setup(s => s.GetRamInfo(out _totalPhysicalMemoryBytes, out _totalVirtualMemoryBytes, out _availablePhysicalMemoryBytes, out _availableVirtualMemoryBytes));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new RamPlugin(systemInfo.Object, dataUploader.Object, logger.Object);

            //Act
            plugin.Execute(metricInstanceId, "", "");

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
