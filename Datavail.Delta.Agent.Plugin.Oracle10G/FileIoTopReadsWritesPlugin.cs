using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using System.Text;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;

namespace Datavail.Delta.Agent.Plugin.Oracle10g
{
    public class FileIoTopReadsWritesPlugin : IPlugin   
    {
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private readonly ISqlRunner _sqlRunner;
        private IDatabaseServerInfo _oracleServerInfo;

        private Guid _metricInstance;
        private string _label;
        private string _output;

        //Specific
        private string _connectionString;
        private string _instanceName;


        public FileIoTopReadsWritesPlugin()
        {
            var common = new Common();
            if (common.GetAgentVersion().Contains("4.0."))
            {
                _dataQueuer = new DataQueuer();
            }
            else
            {
                _dataQueuer = new DotNetDataQueuer();
            }
            _sqlRunner = new SqlServerRunner();
            _logger = new DeltaLogger();
        }

        public FileIoTopReadsWritesPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("FileIoTopReadsWritesPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetTopReads();
                
                _dataQueuer.Queue(_output);
                _logger.LogDebug("Data Queued: " + _output);
                
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }
        }


        private void ParseData(string data)
        {
            var crypto = new Encryption();
            var xmlData = XElement.Parse(data);

            _connectionString = crypto.DecryptString(xmlData.Attribute("ConnectionString").Value);
            _instanceName = xmlData.Attribute("InstanceName").Value;


            if (_oracleServerInfo == null)
                _oracleServerInfo = new OracleServerInfo(_connectionString);
        }


        private void GetTopReads()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("SELECT DISTINCT grantee \"USERNAME\" ");
            sql.AppendLine("FROM dba_role_privs ");
            sql.AppendLine("START WITH granted_role = 'DBA' ");
            sql.AppendLine("CONNECT BY PRIOR grantee = granted_role ");
            sql.AppendLine("minus ");
            sql.AppendLine("SELECT role FROM dba_roles ");
            sql.AppendLine("ORDER BY 1; ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {

                    resultCode = "0";
                    resultMessage = "File IO Top Reads / Writes returned: " + _instanceName;

                    var fileNum = result["FILENUM"].ToString();
                    var file = result["FILE"].ToString();
                    var physicalReads = result["Phys. Reads"].ToString();
                    var averageReadTime = result["Avg. ReadTime"].ToString();
                    var physicalWrites = result["Phys. Writes"].ToString();
                    var averageWriteTime = result["Avg WriteTime"].ToString();

                    BuildExecuteOutput(fileNum, file, physicalReads, averageReadTime, physicalWrites, averageWriteTime, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "File IO Top Reads / Writes not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string fileNum, string file, string physicalReads, string averageReadTime, string physicalWrites, 
            string averageWriteTime, string resultCode, string resultMessage)
        {
            var xml = new XElement("FileIoTopReadsWritesPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("fileNum", fileNum),
                                   new XAttribute("file", file),
                                   new XAttribute("physicalReads", physicalReads),
                                   new XAttribute("averageReadTime", averageReadTime),
                                   new XAttribute("physicalWrites", physicalWrites),
                                   new XAttribute("averageWriteTime", averageWriteTime)
                                   );

            _output = xml.ToString();
        }
    }
}
