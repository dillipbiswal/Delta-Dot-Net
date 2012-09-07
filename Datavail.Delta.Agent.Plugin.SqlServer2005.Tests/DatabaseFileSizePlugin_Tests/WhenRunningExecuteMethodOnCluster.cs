using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.SqlServer2005;
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

namespace Datavail.Delta.Agent.Plugin.SqlServer2005.Tests.DatabaseFileSizePlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethodOnCluster
    {
        private const string MockDatabaseName = "MockDb";
        private const string MockInstanceName = "MockInstanceName";

        #region Helper Methods

        private class TestData
        {
            public string FileName { get; set; }
            public string FileGroupType { get; set; }
            public string FileGroup { get; set; }
            public string InitialSize { get; set; }
            public string SpaceUsed { get; set; }
            public string Growth { get; set; }
            public string Status { get; set; }
            public string MaxSize { get; set; }
            public string Disk { get; set; }
            public string FilePath { get; set; }
        }

        private static IDataReader SetupMockDataReader()
        {
            var testData = new List<TestData>()
                               {
                                   new TestData
                                       {
                                           FileName = "Master",
                                           FileGroupType = "Rows Data",
                                           FileGroup = "PRIMARY",
                                           InitialSize = "2048",
                                           SpaceUsed = "1215",
                                           Growth = "128",
                                           Status = "2",
                                           MaxSize = "-1",
                                           Disk = "C",
                                           FilePath =
                                               @"C:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\DATA\Master.mdf"
                                       },
                                       new TestData
                                       {
                                           FileName = "Master_log",
                                           FileGroupType = "Log",
                                           FileGroup = "Not Applicable",
                                           InitialSize = "1024",
                                           SpaceUsed = "392",
                                           Growth = "10",
                                           Status = "1048642",
                                           MaxSize = "268435456",
                                           Disk = "C",
                                           FilePath =@"C:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\DATA\Master_log.ldf"
                                       }
                               };

            var dataReader = new Mock<IDataReader>();
            var count = -1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(x => x.Read()).Returns(() => count < testData.Count - 1).Callback(() => count++);
            // ReSharper restore AccessToModifiedClosure

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["FileName"]).Returns(() => testData[count].FileName);
            dataReader.Setup(r => r["FileGroupType"]).Returns(() => testData[count].FileGroupType);
            dataReader.Setup(r => r["FileGroup"]).Returns(() => testData[count].FileGroup);
            dataReader.Setup(r => r["InitialSize"]).Returns(() => testData[count].InitialSize);
            dataReader.Setup(r => r["SpaceUsed"]).Returns(() => testData[count].SpaceUsed);
            dataReader.Setup(r => r["Growth"]).Returns(() => testData[count].Growth);
            dataReader.Setup(r => r["Status"]).Returns(() => testData[count].Status);
            dataReader.Setup(r => r["MaxSize"]).Returns(() => testData[count].MaxSize);
            dataReader.Setup(r => r["Disk"]).Returns(() => testData[count].Disk);
            dataReader.Setup(r => r["FilePath"]).Returns(() => testData[count].FilePath);

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
            var plugin = new DatabaseFileSizePlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseFileSizePluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", MockDatabaseName),
                                         new XAttribute("InstanceName", MockInstanceName));

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
            var plugin = new DatabaseFileSizePlugin(clusterInfo.Object, dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("DatabaseFileSizePluginInput",
                                         new XAttribute("ClusterGroupName", "MockClusterGroup"),
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("DatabaseName", MockDatabaseName),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockDatabaseName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.AreNotEqual(String.Empty, uploadedData);
        }
    }
}
