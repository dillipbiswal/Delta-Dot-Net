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
    public class DatabaseInformationPlugin : IPlugin   
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


        public DatabaseInformationPlugin()
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

        public DatabaseInformationPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseInformationPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetDatabaseInformation();
                
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


        private void GetDatabaseInformation()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("SELECT i.instance_number, ");
            sql.AppendLine("i.instance_name, ");
            sql.AppendLine("TO_CHAR(d.created,'MM/DD/YYYY') created, ");
            sql.AppendLine("i.host_name , ");
            sql.AppendLine("i.version , ");
            sql.AppendLine(" TO_CHAR(i.startup_time,'MM/DD/YYYY') startup_time, ");
            sql.AppendLine("i.status , ");
            sql.AppendLine("i.parallel ");
            sql.AppendLine("FROM gv$instance i, ");
            sql.AppendLine("gv$database d ");
            sql.AppendLine("WHERE i.inst_id = d.inst_id; ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var instanceNumber = result["INSTANCE_NUMBER"].ToString();
                    var instanceName = result["INSTANCE_NAME"].ToString();
                    var created = result["CREATED"].ToString();
                    var hostName = result["HOST_NAME"].ToString();
                    var version = result["VERSION"].ToString();
                    var startupTime = result["STARTUP_TIME"].ToString();
                    var status = result["STATUS"].ToString();
                    var parallel = result["PARALLEL"].ToString();
    
                    resultCode = "0";
                    resultMessage = "Database Information return: " + _instanceName;

                    BuildExecuteOutput(instanceNumber, instanceName, created, hostName, version, startupTime, status, parallel, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Database Information not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string instanceNumber, string instanceName, string created, string hostName, string version,
            string startupTime, string status, string parallel,
            string resultCode, string resultMessage)
        {
            var xml = new XElement("DatabaseInformationPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("instanceNumber", instanceNumber),
                                   new XAttribute("instanceName", instanceName),
                                   new XAttribute("created", created),
                                   new XAttribute("hostName", hostName),
                                   new XAttribute("version", version),
                                   new XAttribute("startupTime", startupTime),
                                   new XAttribute("status", status),
                                   new XAttribute("parallel", parallel)
                                   );

            _output = xml.ToString();
        }
    }
}
