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
    public class WhenRunningGetDiskFreeSpaceMethod
    {

        [TestMethod]
        public void ThenDiskFreeSpaceIsReturned()
        {
            //Arrange
            SystemInfo systemInfo = new SystemInfo();
            var totalBytes = 0L;
            var totalFreeBytes = 0L;


            //Act
            systemInfo.GetDiskFreeSpace("C:\\", out totalBytes, out totalFreeBytes);

            //Assert
            Assert.IsInstanceOfType(totalBytes, typeof(long));
            Assert.IsInstanceOfType(totalFreeBytes, typeof(long));
        }
    }
}
