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

namespace Datavail.Delta.Agent.Plugin.SqlServer2008.Tests.DatabaseServerMergeReplicationPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnCluster
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
            dataReader.Setup(r => r["publisher"]).Returns("Test Subscriber");
            dataReader.Setup(r => r["publisher_DB"]).Returns("Test Publication");
            dataReader.Setup(r => r["publication"]).Returns("Running");
            dataReader.Setup(r => r["publication_type"]).Returns("Subscriber DB");


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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlRunnerPublishers, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", MockDatabaseName));

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
            var sqlRunnerPublishers = SetupPublisherMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerMergeReplicationPlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlRunnerPublishers, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerMergeReplicationPluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DistributionDatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }
    }
}
