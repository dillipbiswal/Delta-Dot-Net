using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.SqlServer2000.Tests.DatabaseServerBlockingPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnNonCluster
    {
        const string MockDatabaseName = "MockDb";
        private const string MockInstanceName = "MockInstanceName";

        #region Helper Methods

        private static IDataReader SetupMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["Blocked Time"]).Returns("12:41am");
            dataReader.Setup(r => r["Blocking SPID"]).Returns("100");
            dataReader.Setup(r => r["Blocking SQL"]).Returns("select * from foo");
            dataReader.Setup(r => r["Blocked SPID"]).Returns("101");
            dataReader.Setup(r => r["Blocked SQL"]).Returns("select * from bar");

            return dataReader.Object;
        }

        private static string SetupMockConnectionString()
        {
            var crypto = new Encryption();
            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=pwd;");

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
            return new XElement("DatabaseServerBlockingPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("JobName", "TestJobName"));

        }

        #endregion


        [TestMethod]
        public void ThenDataIsUploaded()
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
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }

        [TestMethod]
        public void ThenNoDatabaseServerBlockingIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            dataReader.Setup(r => r.Read()).Returns(false);
            dataReader.Setup(r => r.FieldCount).Returns(0);

            var sqlRunner = SetupMockSqlRunner(dataReader.Object);
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("resultCode=\"-1\""));
            Assert.IsTrue(uploadedData.Contains("resultMessage=\"No blocking information found for: " + metricInstanceId));

        }

        [TestMethod]
        public void ThenTimestampIsPopulatedInUploadedData()
        {
            //Arrange
            const string timestampRegex = "timestamp=\\\"\\d{4}\\-\\d{2}\\-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{1,10}Z\"";
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(Regex.IsMatch(uploadedData, timestampRegex));
        }

        [TestMethod]
        public void ThenMetricInstanceIdIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("metricInstanceId=\"" + metricInstanceId + "\""));
        }

        [TestMethod]
        public void ThenLabelIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("label=\"" + MockDatabaseName + "\""));
        }

        [TestMethod]
        public void ThenBlockedTimeIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("blockedTime=\"12:41am"));
        }

        [TestMethod]
        public void ThenBlockingSpidIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("blockingSpid=\"100"));
        }

        [TestMethod]
        public void ThenBlockingSqlIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("blockingSql=\"select * from foo"));
        }

        [TestMethod]
        public void ThenBlockedSpidIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("blockedSpid=\"101"));
        }

        [TestMethod]
        public void ThenBlockedSqlIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("blockedSql=\"select * from bar"));
        }

        [TestMethod]
        public void ThenProductIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("product=\"SQL Server"));
        }

        [TestMethod]
        public void ThenProductLevelIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("productLevel=\"SP3"));
        }

        [TestMethod]
        public void ThenProductVersionIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("productVersion=\"9.00.4035.00"));
        }

        [TestMethod]
        public void ThenProductEditionIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var inputData = GetInputData();

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("productEdition=\"Enterprise Edition"));
        }


        [TestMethod]
        public void ThenLogEntryIsWrittenWhenExceptionIsThrown()
        {
            var metricInstanceId = Guid.Empty;

            var clusterInfo = new Mock<IClusterInfo>();
            var common = new Mock<ICommon>();
            var dataUploader = new Mock<IDataQueuer>();
            var sqlRunner = new Mock<ISqlRunner>();

            var logger = new Mock<IDeltaLogger>();

            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerBlockingPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner.Object, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerBlockingPluginInput", new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", "BadDatabaseName"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
