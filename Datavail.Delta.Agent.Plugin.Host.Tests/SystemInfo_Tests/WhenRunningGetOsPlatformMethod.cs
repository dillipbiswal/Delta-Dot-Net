using System;
using System.Text.RegularExpressions;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.Host.Tests.CpuPlugin_Tests
{
    [TestClass]
    public class WhenRunningGetOsPlatformMethod
    {

        [TestMethod]
        public void ThenOsPlatformInfoIsReturned()
        {
            //Arrange
            SystemInfo systemInfo = new SystemInfo();
            var osPlatform = string.Empty;

            //Act
            osPlatform = systemInfo.GetOsPlatform();

            //Assert
            Assert.IsInstanceOfType(osPlatform, typeof(string));

        }
    }
}
