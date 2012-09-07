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
using Datavail.Delta.Infrastructure.Agent.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.SqlServer2005.Tests.DatabaseServerMergeReplicationPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnNonCluster
    {
        const string MockDatabaseName = "MockDb";

        #region Helper Methods

        private static IDataReader SetupMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["subscriber"]).Returns("Test Subscriber");
            dataReader.Setup(r => r["publication"]).Returns("Test Publication");
            dataReader.Setup(r => r["status"]).Returns("Running");
            dataReader.Setup(r => r["subscriber_db"]).Returns("Subscriber DB");
            dataReader.Setup(r => r["type"]).Returns("Merge");
            dataReader.Setup(r => r["agent_name"]).Returns("Test Agent Name");
            dataReader.Setup(r => r["last_action"]).Returns("No data needed to be merged.");
            dataReader.Setup(r => r["start_time"]).Returns("20110505 15:04:18.920");
            dataReader.Setup(r => r["action_time"]).Returns("20110504 16:52:46.480");
            dataReader.Setup(r => r["duration"]).Returns("79892");
            dataReader.Setup(r => r["delivery_rate"]).Returns("0");
            dataReader.Setup(r => r["download_inserts"]).Returns("300");
            dataReader.Setup(r => r["download_updates"]).Returns("500");
            dataReader.Setup(r => r["download_deletes"]).Returns("300");
            dataReader.Setup(r => r["publisher_conficts"]).Returns("500");
            dataReader.Setup(r => r["upload_inserts"]).Returns("300");
            dataReader.Setup(r => r["upload_updates"]).Returns("500");
            dataReader.Setup(r => r["upload_deletes"]).Returns("300");
            dataReader.Setup(r => r["subscriber_conficts"]).Returns("500");
            dataReader.Setup(r => r["error_id"]).Returns("1");
            dataReader.Setup(r => r["job_id"]).Returns("2");
            dataReader.Setup(r => r["local_job"]).Returns("Test Local Job");
            dataReader.Setup(r => r["profile_id"]).Returns("3");
            dataReader.Setup(r => r["agent_id"]).Returns("4");
            dataReader.Setup(r => r["last_timestamp"]).Returns("12344");
            dataReader.Setup(r => r["offload_enabled"]).Returns("1");
            dataReader.Setup(r => r["offload_server"]).Returns("Test Offload Server");
            dataReader.Setup(r => r["subscriber_type"]).Returns("Merge");

            return dataReader.Object;
        }

        private static IDataReader SetupPublishersMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["publisher"]).Returns("Test Publisher");
            dataReader.Setup(r => r["publisher_DB"]).Returns("Test Publisher DB");
            dataReader.Setup(r => r["publication"]).Returns("Test Publication");
            dataReader.Setup(r => r["publication_type"]).Returns("Test Publicatin Type");


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

        private static ISqlRunner SetupPublisherMockSqlRunner(IDataReader dataReader = null)
        {
            if (dataReader == null)
                dataReader = SetupPublishersMockDataReader();

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

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

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distribution Database"));

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("label=\"" + MockDatabaseName + "\""));
        }

        [TestMethod]
        public void ThenNoMergeReplicationDataIsReturnedWhenInvalidDatabaseNameIsRequested()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var clusterInfo = new Mock<IClusterInfo>();
            clusterInfo.Setup(c => c.IsActiveClusterNodeForGroup(It.IsAny<string>())).Returns(false);

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            dataReader.Setup(r => r.Read()).Returns(false);
            dataReader.Setup(r => r.FieldCount).Returns(0);

            var sqlRunner = SetupMockSqlRunner(dataReader.Object);
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DistributionDatabaseName", "Bad Test Distribution DB Name"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains(""));
            //Assert.IsTrue(uploadedData.Contains("Merge Replication data not found for metricinstance " + metricInstanceId +
            //                        " with distribution database: Bad Test Distribution DB Name"));
        }

        [TestMethod]
        public void ThenSubscriberIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("subscriber=\"Test Subscriber"));
        }

        [TestMethod]
        public void ThenPublicationIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("publication=\"Test Publication"));
        }

        [TestMethod]
        public void ThenStatusIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("status=\"Running"));
        }

        [TestMethod]
        public void ThenSubscriberDbIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("subscriberDb=\"Subscriber DB"));
        }

        [TestMethod]
        public void ThenTypeIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("type=\"Merge"));
        }

        [TestMethod]
        public void ThenAgentNameIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("agentName=\"Test Agent Name"));
        }

        [TestMethod]
        public void ThenLastActionIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("lastAction=\""));
        }

        [TestMethod]
        public void ThenStartTimeIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("startTime=\"20110505 15:04:18.920"));
        }

        [TestMethod]
        public void ThenActionTimeIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("actionTime=\"20110504 16:52:46.480"));
        }

        [TestMethod]
        public void ThenDurationIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("duration=\"79892"));
        }

        [TestMethod]
        public void ThenDeliveryRateIsPopulatedInUploadedData()
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("deliveryRate=\"0"));
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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object,
                                                                  logger.Object, sqlRunner, sqlRunnerPublishers, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString), new XAttribute("DistributionDatabaseName", "Test Distributin Database"));

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
            var sqlRunnerPublishers = new Mock<ISqlRunner>();

            var logger = new Mock<IDeltaLogger>();

            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object, sqlRunner.Object, sqlRunnerPublishers.Object, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput", new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", "BadDatabaseName"));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
