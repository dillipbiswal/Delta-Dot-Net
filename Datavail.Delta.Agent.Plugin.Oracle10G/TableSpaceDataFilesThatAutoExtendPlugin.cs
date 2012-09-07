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
    public class TableSpaceDataFilesThatAutoExtendPlugin : IPlugin   
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


        public TableSpaceDataFilesThatAutoExtendPlugin()
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

        public TableSpaceDataFilesThatAutoExtendPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("TableSpaceDataFilesThatAutoExtendPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetTableSpaceDataFilesThatAutoExtend();
                
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


        private void GetTableSpaceDataFilesThatAutoExtend()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("select TABLESPACE_NAME, substr(file_name,1,4) \"Filesystem\", autoextensible ");
            sql.AppendLine("from dba_data_files ");
            sql.AppendLine("group by TABLESPACE_NAME, substr(file_name,1,4), autoextensible ");
            sql.AppendLine("order by TABLESPACE_NAME, substr(file_name,1,4); ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {

                    resultCode = "0";
                    resultMessage = "Table Space Data Files That Auto Extend returned: " + _instanceName;

                    var tableSpaceName = result["TABLESPACE_NAME"].ToString();
                    var filesystem = result["Filesystem"].ToString();
                    var autoExtensible = result["autoextensible"].ToString();

                    BuildExecuteOutput(tableSpaceName, filesystem, autoExtensible, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Table Space Data Files That Auto Extend not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string tableSpaceName, string filesystem, string autoExtensible, string resultCode, string resultMessage)
        {
            var xml = new XElement("TableSpaceDataFilesThatAutoExtendPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("tableSpaceName", tableSpaceName),
                                   new XAttribute("filesystem", filesystem),
                                   new XAttribute("autoExtensible", autoExtensible)
                                   );

            _output = xml.ToString();
        }
    }
}
