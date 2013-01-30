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
    public class BlockingStatusPlugin : IPlugin   
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


        public BlockingStatusPlugin()
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

        public BlockingStatusPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("BlockingStatusPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetInstanceStatus();
                
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


        private void GetInstanceStatus()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("SELECT to_char(s.seconds_in_wait) wait, ");
            sql.AppendLine("to_char(s.blocking_session) blking_id, ");
            sql.AppendLine("to_char(s.sid) curr_id, ");
            sql.AppendLine("DECODE (l.locked_mode, ");
            sql.AppendLine("     1, ");
            sql.AppendLine("     'No Lock', ");
            sql.AppendLine("     2, ");
            sql.AppendLine("     'Row Share', ");
            sql.AppendLine("     3, ");
            sql.AppendLine("     'Row Exclusive', ");
            sql.AppendLine("     4, ");
            sql.AppendLine("     'Shared Table', ");
            sql.AppendLine("     5, ");
            sql.AppendLine("     'Shared Row Exclusive', ");
            sql.AppendLine("     6, ");
            sql.AppendLine("     'Exclusive') ");
            sql.AppendLine("lock_type, ");
            sql.AppendLine("s.ROW_WAIT_OBJ# obj#, ");
            sql.AppendLine("s.ROW_WAIT_FILE# file#, ");
            sql.AppendLine("s.ROW_WAIT_BLOCK# block#, ");
            sql.AppendLine("s.ROW_WAIT_ROW# row#, ");
            sql.AppendLine("do.owner || '.' || do.object_name object, ");
            sql.AppendLine("'SELECT * FROM ' ");
            sql.AppendLine("|| do.owner ");
            sql.AppendLine("|| '.' ");
            sql.AppendLine("|| do.object_name ");
            sql.AppendLine("|| ' WHERE rowid = ' ");
            sql.AppendLine("|| DBMS_ROWID.rowid_create (1, ");
            sql.AppendLine("                         s.ROW_WAIT_OBJ#, ");
            sql.AppendLine("                         s.ROW_WAIT_FILE#, ");
            sql.AppendLine("                         s.ROW_WAIT_BLOCK#, ");
            sql.AppendLine("                         s.ROW_WAIT_ROW#) ");
            sql.AppendLine("|| ';' ");
            sql.AppendLine("sql_to_find_locked_row, ");
            sql.AppendLine("s.sql_id running_sqlid, ");
            sql.AppendLine("substr(sa.sql_text, 1, 130) running_sql ");
            sql.AppendLine("FROM   v$session s, dba_objects do, v$locked_object l, v$sqlarea sa ");
            sql.AppendLine("WHERE       blocking_session IS NOT NULL ");
            sql.AppendLine(" AND s.ROW_WAIT_OBJ# = do.OBJECT_ID ");
            sql.AppendLine("AND l.session_id = s.sid ");
            sql.AppendLine("AND sa.sql_id = s.sql_id ");
            sql.AppendLine("AND s.seconds_in_wait > 0 ");
            sql.AppendLine("order by s.seconds_in_wait desc, obj# ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var wait = result["WAIT"].ToString();
                    var blkingId = result["BLKING_ID"].ToString();
                    var currId = result["CURR_ID"].ToString();
                    var lockType = result["LOCK_TYPE"].ToString();
                    var objNum = result["OBJ#"].ToString();
                    var fileNum = result["FILE#"].ToString();
                    var blockNum = result["BLOCK#"].ToString();
                    var rowNum = result["ROW#"].ToString();
                    var obj = result["OBJECT"].ToString();
                    var sqlToFindLockedRow = result["SQL_TO_FIND_LOCKED_ROW"].ToString();
                    var runningSqlId = result["RUNNING_SQLID"].ToString();
                    var runningSql = result["RUNNING_SQL"].ToString();

                    resultCode = "0";
                    resultMessage = "Blocking status " + _instanceName;

                    BuildExecuteOutput(wait, blkingId, currId, lockType, objNum, fileNum, blockNum, rowNum, obj, sqlToFindLockedRow,
                        runningSqlId, runningSql, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Blocking status (RMAN) not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", "", "", "", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string wait, string blkingId, string currId, string lockType, string objNum, string fileNum,
            string blockNum, string rowNum, string obj, string sqlToFindLockedRow, string runningSqlId, string runningSql,
            string resultCode, string resultMessage)
        {
            var xml = new XElement("BlockingStatusRmanPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("wait", wait),
                                   new XAttribute("blkingId", blkingId),
                                   new XAttribute("currId", currId),
                                   new XAttribute("lockType", lockType),
                                   new XAttribute("objNum", objNum),
                                   new XAttribute("fileNum", fileNum),
                                   new XAttribute("blockNum", blockNum),
                                   new XAttribute("rowNum", rowNum),
                                   new XAttribute("obj", obj),
                                   new XAttribute("sqlToFindLockedRow", sqlToFindLockedRow),
                                   new XAttribute("runningSqlId", runningSqlId),
                                   new XAttribute("runningSql", runningSql)
                                   );

            _output = xml.ToString();
        }
    }
}
