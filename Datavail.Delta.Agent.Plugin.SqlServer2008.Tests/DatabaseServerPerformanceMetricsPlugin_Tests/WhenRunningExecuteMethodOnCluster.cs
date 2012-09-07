using System;
using System.Collections.Generic;
using System.Data;
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

namespace Datavail.Delta.Agent.Plugin.SqlServer2008.Tests.DatabaseServerPerformanceMetricsPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnCluster
    {
        private const string MockDatabaseName = "MockDb";
        private const string MockInstanceName = "MockInstanceName";

        #region Helper Methods

        private static IDataReader SetupMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 11;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure
            /*
            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["Batch Requests/sec"]).Returns("100");
            dataReader.Setup(r => r["Checkpoint pages/sec"]).Returns("200");
            dataReader.Setup(r => r["Lazy writes/sec"]).Returns("300");
            dataReader.Setup(r => r["Log Flushes/sec"]).Returns("400");
            dataReader.Setup(r => r["Page life expectancy"]).Returns("500");
            dataReader.Setup(r => r["Page lookups/sec"]).Returns("600");
            dataReader.Setup(r => r["Page Splits/sec"]).Returns("700");
            dataReader.Setup(r => r["SQL Compilations/sec"]).Returns("800");
            dataReader.Setup(r => r["Target Server Memory (KB)"]).Returns("900");
            dataReader.Setup(r => r["Total Server Memory (KB)"]).Returns("1000");
            dataReader.Setup(r => r["Transactions/sec"]).Returns("1100");
            */

            var objNames = new Queue<string>(new string[] { "MSSQL$MMV:SQL Statistics", "MSSQL$MMV:Buffer Manager", "MSSQL$MMV:Buffer Manager", "MSSQL$MMV:Databases", "MSSQL$MMV:Buffer Manager", "MSSQL$MMV:Buffer Manager", "MSSQL$MMV:Access Methods", "MSSQL$MMV:SQL Statistics", "MSSQL$MMV:Memory Manager", "MSSQL$MMV:Memory Manager", "MSSQL$MMV:Databases" });
            var counterNames = new Queue<string>(new string[] { "Batch Requests/sec", "Checkpoint pages/sec", "Lazy writes/sec", "Log Flushes/sec", "Page life expectancy", "Page lookups/sec", "Page Splits/sec", "SQL Compilations/sec", "Target Server Memory (KB)", "Total Server Memory (KB)", "Transactions/sec" });
            var counterValues = new Queue<string>(new string[] { "100", "200", "300", "400", "500", "600", "700", "800", "900", "1000", "1100" });
            var instanceNames = new Queue<string>(new string[] { "", "", "", "", "", "", "", "", "", "", "" });
            var counterType = new Queue<string>(new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });

            dataReader.Setup(r => r.FieldCount).Returns(count);
            dataReader.Setup(r => r["object_name"]).Returns(() => objNames.Dequeue());
            dataReader.Setup(r => r["counter_name"]).Returns(() => counterNames.Dequeue());
            dataReader.Setup(r => r["instance_name"]).Returns(() => instanceNames.Dequeue());
            dataReader.Setup(r => r["cntr_value"]).Returns(() => counterValues.Dequeue());
            dataReader.Setup(r => r["cntr_type"]).Returns(() => counterType.Dequeue());

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
            var plugin = new DatabaseServerPerformanceCountersPlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerPerformanceCountersPluginInput",
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
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new DatabaseServerPerformanceCountersPlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseServerPerformanceCountersPluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName),
                                         new XAttribute("DatabaseName", MockDatabaseName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }
    }
}
