using System;
using System.IO;
using System.Text.RegularExpressions;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.CpuPlugin_Tests
{
    [TestClass]
    public class WhenRunningGetDriveInfoMethod
    {

        [TestMethod]
        public void ThenValueAsFloatIsReturned()
        {
            //Arrange
            SystemInfo systemInfo = new SystemInfo();
            var driveType = new DriveType();
            var driveFormat = string.Empty;
            var volumeLabel = string.Empty;
            var totalSize = 0L;

            //Act
            systemInfo.GetDriveInfo("C:\\", out driveType, out driveFormat, out totalSize, out volumeLabel);

            //Assert
            Assert.IsInstanceOfType(driveType, typeof(DriveType));
            Assert.IsInstanceOfType(driveFormat, typeof(string));
            Assert.IsInstanceOfType(volumeLabel, typeof(string));
        }
    }
}
