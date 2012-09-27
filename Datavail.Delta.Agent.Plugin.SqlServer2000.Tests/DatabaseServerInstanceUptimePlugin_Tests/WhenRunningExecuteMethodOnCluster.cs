using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Xml.Linq;

namespace Datavail.Delta.Agent.Plugin.SqlServer2000.Tests.SqlAgentStatusPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnCluster
    {
        private const string MockDatabaseName = "MockDb";
        private const string MockInstanceName = "MockDb";

        #region Helper Methods

        private static IDataReader SetupMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["sqlinstanceuptime"]).Returns("18d 3h 7m");
            dataReader.Setup(r => r["sqlagentstatus"]).Returns("Running.");


            return dataReader.Object;
        }

        private static string SetupMockConnectionString()
        {
            var crypto = new Encryption();
            var connectionString =
                crypto.EncryptToString(
                    @"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");

            return connectionString;
        }

        private static ISqlRunner SetupMockSqlRunner(IDataReader dataReader = null)
        {
            if (dataReader == null)
                dataReader = SetupMockDataReader();

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader);

            return sqlRunner.Object;
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

        private static XElement GetInputData()
        {
            var connectionString = SetupMockConnectionString();
            return new XElement("SqlAgentStatusPluginInput",
                                          new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                          new XAttribute("ConnectionString", connectionString),
                                          new XAttribute("DatabaseName", MockDatabaseName),
                                          new XAttribute("InstanceName", MockInstanceName));
        }

        #endregion

        [TestMethod]
        public void ThenDataIsNotUploadedWhenNotActiveNode()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new SqlAgentStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            var additionalProgramNames = new XElement("ProgramNames", new XElement("ProgramName", "ProgramName1"), new XElement("ProgramName", "ProgramName2"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Never());
            Assert.AreEqual(String.Empty, uploadedData);
        }

        [TestMethod]
        public void ThenDataIsUploadedWhenActiveNode()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(true);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new SqlAgentStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var inputData = GetInputData();


            var additionalProgramNames = new XElement("ProgramNames", new XElement("ProgramName", "ProgramName1"), new XElement("ProgramName", "ProgramName2"));
            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }
    }
}
