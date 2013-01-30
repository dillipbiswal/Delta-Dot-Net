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
    public class TableSpacePercentageUsedPlugin : IPlugin   
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


        public TableSpacePercentageUsedPlugin()
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

        public TableSpacePercentageUsedPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("TableSpacePercentageUsedPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetUserRolesGranted();
                
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


        private void GetUserRolesGranted()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("select ");
            sql.AppendLine("e.tablespace_name tablespace_name, ");
            sql.AppendLine("round(c.total_bytes/1024/1024) total_mb, ");
            sql.AppendLine("nvl(round(a.used_bytes/1024/1024),0) used_mb, ");
            sql.AppendLine("round(b.free_bytes/1024/1024) free_mb, ");
            sql.AppendLine("nvl(round(100*(a.used_bytes)/(c.total_bytes),0),0) percent_used, ");
            sql.AppendLine("d.status status ");
            sql.AppendLine("from ");
            sql.AppendLine("( ");
            sql.AppendLine("select tablespace_name ");
            sql.AppendLine("from dba_tablespaces ");
            sql.AppendLine(") e, ");
            sql.AppendLine("( ");
            sql.AppendLine("select tablespace_name, sum(bytes) total_bytes ");
            sql.AppendLine("from dba_data_files ");
            sql.AppendLine("group by tablespace_name ");
            sql.AppendLine(") c, ");
            sql.AppendLine("( ");
            sql.AppendLine("select tablespace_name, sum(bytes) used_bytes ");
            sql.AppendLine("from dba_segments ");
            sql.AppendLine("group by tablespace_name ");
            sql.AppendLine(") a, ");
            sql.AppendLine("( ");
            sql.AppendLine("select tablespace_name, sum(bytes) free_bytes ");
            sql.AppendLine("from dba_free_space ");
            sql.AppendLine("group by tablespace_name ");
            sql.AppendLine(") b, ");
            sql.AppendLine("( ");
            sql.AppendLine("select tablespace_name, status ");
            sql.AppendLine("from dba_tablespaces ");
            sql.AppendLine(") d ");
            sql.AppendLine("where e.TABLESPACE_NAME=c.TABLESPACE_NAME ");
            sql.AppendLine("and e.TABLESPACE_NAME=b.TABLESPACE_NAME ");
            sql.AppendLine("and e.TABLESPACE_NAME=a.TABLESPACE_NAME (+) ");
            sql.AppendLine("and e.TABLESPACE_NAME=d.TABLESPACE_NAME ");
            sql.AppendLine("order by percent_used desc; ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {

                    resultCode = "0";
                    resultMessage = "TableSpace Percentage Used returned: " + _instanceName;

                    var tableSpaceName = result["TABLESPACE_NAME"].ToString();
                    var totalMb = result["TOTAL_MB"].ToString();
                    var usedMb = result["USED_MB"].ToString();
                    var freeMb = result["FREE_MB"].ToString();
                    var percentUsed = result["PERCENT_USED"].ToString();
                    var status = result["STATUS"].ToString();

                    BuildExecuteOutput(tableSpaceName, totalMb, usedMb, freeMb, percentUsed, status, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "TableSpace Percentage Used not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string tableSpaceName, string totalMb, string usedMb, string freeMb, string percentUsed, string status, 
            string resultCode, string resultMessage)
        {
            var xml = new XElement("TableSpacePercentageUsedPluginOutput",
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
                                   new XAttribute("totalMb", totalMb),
                                   new XAttribute("usedMb", usedMb),
                                   new XAttribute("freeMb", freeMb),
                                   new XAttribute("percentUsed", percentUsed),
                                   new XAttribute("status", status)
                                   );

            _output = xml.ToString();
        }
    }
}
