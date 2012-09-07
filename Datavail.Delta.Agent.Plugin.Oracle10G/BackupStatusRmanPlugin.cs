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
    public class BackupStatusRmanPlugin : IPlugin   
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


        public BackupStatusRmanPlugin()
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

        public BackupStatusRmanPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("BackupStatusRmanPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

            sql.Append("Select operation,session_recid,session_stamp,parent_recid,parent_stamp,status,object_type,start_time");
            sql.Append(",end_time,input_bytes,output_bytes from v$rman_status where start_time >= ? "); 
		  	sql.Append("order by start_time");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var operation = result["operation"].ToString();
                    var status = result["status"].ToString();
                    var sessionRecId = result["session_recid"].ToString();
                    var sessionStamp = result["session_stamp"].ToString();
                    var parentRecId = result["parent_recid"].ToString();
                    var parentStamp = result["parent_stamp"].ToString();
                    var objectType = result["object_type"].ToString();
                    var inputBytes = result["input_bytes"].ToString();
                    var outputBytes = result["output_bytes"].ToString();
                    var startTime = result["start_time"].ToString();
                    var endTime = result["end_time"].ToString();

                    resultCode = "0";
                    resultMessage = "Status returned for database: " + _instanceName;

                    BuildExecuteOutput(operation, status, sessionRecId, sessionStamp, parentRecId, parentStamp, 
                        objectType, inputBytes, outputBytes, startTime, endTime, resultCode, resultMessage);
                }
                
            }
            else
            {
                resultMessage = "Backup Status (RMAN) not returned: " + _instanceName;
              //  BuildExecuteOutput("Down", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string operation, string status, string sessionRecId, string sessionStamp, string parentRecId, string parentStamp,
                        string objectType, string inputBytes, string outputBytes, string startTime, string endTime, string resultCode, string resultMessage)
        {
            var xml = new XElement("BackupStatusRmanPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("operation", operation),
                                   new XAttribute("status", status),
                                   new XAttribute("startTime", startTime),
                                   new XAttribute("sessionRecId", sessionRecId),
                                   new XAttribute("sessionStamp", sessionStamp),
                                   new XAttribute("parentRecId", parentRecId),
                                   new XAttribute("parentStamp", parentStamp),
                                   new XAttribute("objectType", objectType),
                                   new XAttribute("endTime", endTime),
                                   new XAttribute("inputBytes", inputBytes),
                                   new XAttribute("outputBytes", outputBytes)
                                   );


            _output = xml.ToString();
        }
    }
}
