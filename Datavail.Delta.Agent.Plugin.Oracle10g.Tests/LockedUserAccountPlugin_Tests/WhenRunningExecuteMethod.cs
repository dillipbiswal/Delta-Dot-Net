using System;
using System.Data;
using System.Xml.Linq;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.Oracle10g.Tests.LockedUserAccountsPlugin_Tests
{
    [TestClass]
    public class WhenRunningExecuteMethod
    {
        private const string MockInstanceName = "MockInstance";

        #region Helper Methods

        private static IDataReader SetupMockDataReader()
        {
            var dataReader = new Mock<IDataReader>();
            var count = 1;

            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure

            dataReader.Setup(r => r.FieldCount).Returns(1);
            dataReader.Setup(r => r["USERNAME"]).Returns("SCOTT");
            dataReader.Setup(r => r["ACCOUNT_STATUS"]).Returns("LOCKED(TIMED)");

            return dataReader.Object;
        }

        private static string SetupMockConnectionString()
        {
            var crypto = new Encryption();
            var connectionString =
                crypto.EncryptToString(
                    @"Data Source=oracleserver;User Id=sa;Password=somepassword!;");

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

            sqlServerInfo.Setup(s => s.Product).Returns("Oracle");
            sqlServerInfo.Setup(s => s.ProductVersion).Returns("10g");
            sqlServerInfo.Setup(s => s.ProductLevel).Returns("something");
            sqlServerInfo.Setup(s => s.ProductEdition).Returns("Oracle 10g");

            return sqlServerInfo.Object;
        }

        #endregion

        [TestMethod]
        public void ThenDataIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();




            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert
            dataUploader.Verify(m => m.Queue(It.IsAny<string>()), Times.Once());
            Assert.IsNotNull(uploadedData);
        }

        [TestMethod]
        public void ThenUserNameIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();

            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("userName=\"SCOTT"));
        }

        [TestMethod]
        public void ThenAccountStatusIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();

            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
                                 
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object,
                                                  sqlRunner, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("accountStatus=\"LOCKED(TIMED)"));
        }

        [TestMethod]
        public void ThenProductIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();




            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("product=\"Oracle"));
        }

        [TestMethod]
        public void ThenProductLevelIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();




            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("productLevel=\"something"));
        }

        [TestMethod]
        public void ThenProductVersionIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();




            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("productVersion=\""));
        }

        [TestMethod]
        public void ThenEditionIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();




            var common = new Mock<ICommon>();

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var sqlRunner = SetupMockSqlRunner();
            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object, sqlRunner, sqlServerInfo);

            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert

            Assert.IsTrue(uploadedData.Contains("productEdition=\""));
        }

        [TestMethod]
        public void ThenLogEntryIsWrittenWhenExceptionIsThrown()
        {
            var metricInstanceId = Guid.Empty;


            var common = new Mock<ICommon>();
            var dataUploader = new Mock<IDataQueuer>();
            var sqlRunner = new Mock<ISqlRunner>();

            var logger = new Mock<IDeltaLogger>();
            var sqlServerInfo = SetupMockSqlServerInfo();
            var plugin = new LockedUserAccountsPlugin(dataUploader.Object, logger.Object, sqlRunner.Object, sqlServerInfo);
            var connectionString = SetupMockConnectionString();
            var inputData = new XElement("LockedUserAccountsPluginInput",
                                         new XAttribute("ConnectionString", connectionString),
                                         new XAttribute("InstanceName", MockInstanceName));

            //Act
            plugin.Execute(metricInstanceId, MockInstanceName, inputData.ToString());

            //Assert
            logger.Verify(l => l.LogUnhandledException(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once());
        }

    }
}
