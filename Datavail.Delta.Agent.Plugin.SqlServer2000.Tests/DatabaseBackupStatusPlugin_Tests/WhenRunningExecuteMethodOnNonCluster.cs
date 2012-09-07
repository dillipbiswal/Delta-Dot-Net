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

namespace Datavail.Delta.Agent.Plugin.SqlServer2000.Tests.DatabaseBackupStatusPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnNonCluster
    {
        const string MockDatabaseName = "MockDb";
        const string MockInstanceName = "Mock Instance";

        #region Helper Methods

        private static IDataReader SetupMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure
            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["database_name"]).Returns("Master");
            dataReader.Setup(r => r["physical_device_name"]).Returns("T:\\MSSQL10.DELTASQL01\\MSSQL\\master\\master_backup.bak");
            dataReader.Setup(r => r["FinishTime"]).Returns("00:00:02");
            dataReader.Setup(r => r["backup_finish_date"]).Returns("2011-07-12 00:00:02.000");
            dataReader.Setup(r => r["MinsSinceLast"]).Returns("752");

            return dataReader.Object;
        }

        private static string SetupMockConnectionString()
        {
            var crypto = new Encryption();
            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");

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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }

        [TestMethod]
        public void ThenBackupNotFoundIsUploaded()
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", "DatabaseNameNotBackedUp"));
            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("resultCode=\"-1\""));
            Assert.IsTrue(uploadedData.Contains("resultMessage=\"No backup history found for database: DatabaseNameNotBackedUp\""));
            Assert.IsTrue(uploadedData.Contains("physicalDeviceName=\"N/A\""));
            Assert.IsTrue(uploadedData.Contains("backupFinishTimeStamp=\""+DateTime.MinValue.ToString()+"\""));
            Assert.IsTrue(uploadedData.Contains("minsSinceLast=\"-1\""));
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));
            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("label=\"" + MockDatabaseName + "\""));
        }

        [TestMethod]
        public void ThenDatabaseNameIsPopulatedInUploadedData()
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("name=\"" + MockDatabaseName + "\""));
        }

        [TestMethod]
        public void ThenPhysicalDeviceNameIsPopulatedInUploadedData()
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("physicalDeviceName=\"T:\\MSSQL10.DELTASQL01\\MSSQL\\master\\master_backup.bak"));
        }


        [TestMethod]
        public void ThenBackupFinishDateIsPopulatedInUploadedData()
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("backupFinishTimeStamp=\"7/12/2011 12:00:02 AM\""));
        }

        [TestMethod]
        public void ThenMinsSinceLastIsPopulatedInUploadedData()
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("minsSinceLast=\"752"));
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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseBackupStatusPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("InstanceName", MockInstanceName), new XAttribute("DatabaseName", MockDatabaseName));

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
            var plugin = new DatabaseBackupStatusPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner.Object, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseStatusPluginInput", new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", "BadDatabaseName"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
