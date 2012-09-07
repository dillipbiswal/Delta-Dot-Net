using System;
using System.Text.RegularExpressions;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.CpuPlugin_Tests
{
    [TestClass]
    public class WhenRunningGetRamInfoMethod
    {

        [TestMethod]
        public void ThenRamInfoIsReturned()
        {
            //Arrange
            SystemInfo systemInfo = new SystemInfo();
            var totalPhysicalMemory = 0UL;
            var totalVirtualMemory = 0UL;
            var availablePhysicalMemory = 0UL;
            var availableVirtualMemory = 0UL;


            //Act
            systemInfo.GetRamInfo(out totalPhysicalMemory, out totalVirtualMemory, out availablePhysicalMemory, out availableVirtualMemory);

            //Assert
            Assert.IsInstanceOfType(totalPhysicalMemory, typeof(ulong));
            Assert.IsInstanceOfType(totalVirtualMemory, typeof(ulong));
            Assert.IsInstanceOfType(availablePhysicalMemory, typeof(ulong));
            Assert.IsInstanceOfType(availableVirtualMemory, typeof(ulong));
        }
    }
}
