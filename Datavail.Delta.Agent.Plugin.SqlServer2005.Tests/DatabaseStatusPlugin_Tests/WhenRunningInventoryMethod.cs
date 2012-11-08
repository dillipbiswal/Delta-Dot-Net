using System;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Datavail.Delta.Agent.Plugin.SqlServer2005.Tests.DatabaseStatusPlugin_Tests
{
    [TestClass]
    public class WhenRunningInventoryMethod
    {
        [TestMethod]
        public void ThenDataIsUploaded()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();
            var crypto = new Encryption();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>(data => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            var count = 1;
            // ReSharper disable AccessToModifiedClosure
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            // ReSharper restore AccessToModifiedClosure
            dataReader.Setup(r => r.RecordsAffected).Returns(1);
            dataReader.Setup(r => r["name"]).Returns("Master");

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader.Object);

            var plugin = new DatabaseStatusPlugin(dataUploader.Object, common.Object, sqlRunner.Object);

            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");
            const string databaseName = "Master";

            var inputData = new XElement("DatabaseStatusInput",
                 new XAttribute("ConnectionString", connectionString),
                 new XAttribute("DatabaseName", databaseName));

            //Act
            plugin.Inventory(metricInstanceId, "Inventory", inputData.ToString());

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
            var crypto = new Encryption();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            var count = 1;
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            dataReader.Setup(r => r.RecordsAffected).Returns(1);
            dataReader.Setup(r => r["database_id"]).Returns(1);
            dataReader.Setup(r => r["state_desc"]).Returns("ONLINE");

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader.Object);

            var plugin = new DatabaseStatusPlugin(dataUploader.Object, common.Object, sqlRunner.Object);


            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");
            const string databaseName = "Master";

            var inputData = new XElement("DatabaseStatusInput",
                 new XAttribute("ConnectionString", connectionString),
                 new XAttribute("DatabaseName", databaseName));

            //Act
            plugin.Execute(metricInstanceId, "Master", inputData.ToString());


            //Assert
            Assert.IsTrue(Regex.IsMatch(uploadedData, timestampRegex));
        }

        [TestMethod]
        public void ThenMetricInstanceIdIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();
            var crypto = new Encryption();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            var count = 1;
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            dataReader.Setup(r => r.RecordsAffected).Returns(1);
            dataReader.Setup(r => r["name"]).Returns("Master");

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader.Object);

            var plugin = new DatabaseStatusPlugin(dataUploader.Object, common.Object, sqlRunner.Object);

            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");
            const string databaseName = "Master";

            var inputData = new XElement("DatabaseStatusInput",
                 new XAttribute("ConnectionString", connectionString),
                 new XAttribute("DatabaseName", databaseName));

            //Act
            plugin.Inventory(metricInstanceId, "Master", inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("metricInstanceId=\"" + metricInstanceId + "\""));
        }

        [TestMethod]
        public void ThenLabelIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();
            var crypto = new Encryption();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            var count = 1;
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            dataReader.Setup(r => r.RecordsAffected).Returns(1);
            dataReader.Setup(r => r["name"]).Returns("Master");

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader.Object);

            var plugin = new DatabaseStatusPlugin(dataUploader.Object, common.Object, sqlRunner.Object);


            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");
            const string databaseName = "Master";

            var inputData = new XElement("DatabaseStatusInput",
                 new XAttribute("ConnectionString", connectionString),
                 new XAttribute("DatabaseName", databaseName));

            //Act
            plugin.Inventory(metricInstanceId, "Inventory", inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("label=\"Inventory\""));
        }

        [TestMethod]
        public void ThenDatabaseNameIsPopulatedInUploadedData()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();
            var crypto = new Encryption();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            var count = 1;
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() => count--);
            dataReader.Setup(r => r.RecordsAffected).Returns(1);
            dataReader.Setup(r => r["name"]).Returns("Master");

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader.Object);

            var plugin = new DatabaseStatusPlugin(dataUploader.Object, common.Object, sqlRunner.Object);


            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");
            const string databaseName = "Master";

            var inputData = new XElement("DatabaseStatusInput",
                 new XAttribute("ConnectionString", connectionString),
                 new XAttribute("DatabaseName", databaseName));

            //Act
            plugin.Inventory(metricInstanceId, "Inventory", inputData.ToString());

            //Assert
            Assert.IsTrue(uploadedData.Contains("Database name=\"Master\""));
        }


        [TestMethod]
        public void ThenOneDatabaseNodeIsGeneratedForEachDatabase()
        {
            //Arrange
            var metricInstanceId = Guid.NewGuid();
            var crypto = new Encryption();

            var common = new Mock<ICommon>();
            common.Setup(c => c.GetServerId()).Returns(new Guid("{880A7147-722B-46C4-ADD0-D33C74DD9573}"));

            var uploadedData = String.Empty;
            var dataUploader = new Mock<IDataQueuer>();
            dataUploader.Setup(u => u.Queue(It.IsAny<string>())).Callback<string>((data) => uploadedData = data);

            var dataReader = new Mock<IDataReader>();
            var count = 3;
            var names = new[] { "Master", "Db1", "Db2" };
            
            dataReader.Setup(r => r.Read()).Returns(() => count > 0).Callback(() =>
            {
                count--;
                if(count>=0)dataReader.Setup(r => r["name"]).Returns(names[count]);
            });
            dataReader.Setup(r => r.RecordsAffected).Returns(3);
            

            var sqlRunner = new Mock<ISqlRunner>();
            sqlRunner.Setup(r => r.RunSql(It.IsAny<string>(), It.IsAny<string>())).Returns(dataReader.Object);

            var plugin = new DatabaseStatusPlugin(dataUploader.Object, common.Object, sqlRunner.Object);


            var connectionString = crypto.EncryptToString(@"Data Source=localhost;Initial Catalog=master;User Id=sa;Password=somepassword!;");
            const string databaseName = "Master";

            var inputData = new XElement("DatabaseStatusInput",
                 new XAttribute("ConnectionString", connectionString),
                 new XAttribute("DatabaseName", databaseName));

            //Act
            plugin.Inventory(metricInstanceId, "Inventory", inputData.ToString());
            var xml = XElement.Parse(uploadedData);

            //Assert
            Assert.IsTrue(xml.Nodes().Count()==3);
        }
    }
}
