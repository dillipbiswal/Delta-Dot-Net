using System;
using System.Xml.Linq;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.MsCluster.Tests.MsClusterGroupStatusPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethod
    {


        #region Helper Methods

        private static IClusterInfo SetupMockClusterInfo()
        {

            var clusterInfo = new Mock<IClusterInfo>();

            clusterInfo.Setup(s => s.GroupStatus).Returns("online");
            clusterInfo.Setup(s => s.NodeStatus).Returns("up");

            return clusterInfo.Object;
        }

        private static string SetupMockConnectionString()
        {
            var crypto = new Encryption();
            var connectionString =
                crypto.EncryptToString(
                    @"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");

            return connectionString;
        }

        private static IDatabaseServerInfo SetupMockSqlServerInfo()
        {

            var sqlServerInfo = new Mock<IDatabaseServerInfo>();

            sqlServerInfo.Setup(s => s.Product).Returns("SQL Server");
            sqlServerInfo.Setup(s => s.ProductVersion).Returns("9.00.4035.00");
            sqlServerInfo.Setup(s => s.ProductLevel).Returns("SP3");
            sqlServerInfo.Setup(s => s.ProductEdition).Returns("Enterprise Edition");

            return sqlServerInfo.Object;
        }

        #endregion

        [TestMethod]
        public void ThenDataIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            var clusterInfrastructure = new Mock<IClusterInfrastructure>();
            
            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new MsClusterGroupStatusPlugin(clusterInfo.Object, clusterInfrastructure.Object, dataUploader.Object, logger.Object);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("MsClusterGroupStatusPluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", "MockDatabaseName"));

            //Act
            plugin.Execute(metricInstanceId, "Cluster Group Status", inputData.ToString());

            //Assert
            //dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            //Assert.AreNotEqual(String.Empty, uploadedData);
        }

        [TestMethod]
        public void ThenGroupStatusIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            var clusterInfrastructure = new Mock<IClusterInfrastructure>();
            
            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var logger = new Mock<IDeltaLogger>();

            var plugin = new MsClusterGroupStatusPlugin(clusterInfo.Object, clusterInfrastructure.Object, dataUploader.Object, logger.Object);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("MsClusterGroupStatusPluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", "MockDatabaseName"));

            //Act
            plugin.Execute(metricInstanceId, "Cluster Group Status", inputData.ToString());
        }
    }
}
