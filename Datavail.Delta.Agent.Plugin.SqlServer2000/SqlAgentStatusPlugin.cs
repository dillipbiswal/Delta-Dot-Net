using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.SqlServer2000.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;


namespace Datavail.Delta.Agent.Plugin.SqlServer2000
{
    public class SqlAgentStatusPlugin : IPlugin
    {
        private readonly IClusterInfo _clusterInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private readonly ISqlRunner _sqlRunner;
        private IDatabaseServerInfo _databaseServerInfo;

        private Guid _metricInstance;
        private string _label;
        private string _output;

        //Specific
        private string _connectionString;
        private string _clusterGroupName;
        private string _instanceName;
        private bool _runningOnCluster = false;
        private IEnumerable<XElement> _programNames;
        
        public SqlAgentStatusPlugin()
        {
            _clusterInfo = new ClusterInfo();
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

        public SqlAgentStatusPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("SqlAgentStatus.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                           metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                if (!_runningOnCluster || (_runningOnCluster && _clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName)))
                {
                    if (_databaseServerInfo == null)
                        _databaseServerInfo = new SqlServerInfo(_connectionString);

                    GetDatabaseStatus();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: SQ; Agent Status not returned.");
                    }
                }
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
            _programNames = xmlData.Elements("ProgramNames").Elements("ProgramName");
            _instanceName = xmlData.Attribute("InstanceName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void GetDatabaseStatus()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("USE master ");
            sql.Append("SET NOCOUNT ON ");
            sql.Append("DECLARE @crdate DATETIME, ");
            sql.Append("@days int, ");
            sql.Append("@hr int, ");
            sql.Append("@min int, ");
            sql.Append("@today DATETIME ");
            sql.Append("declare @sqlagentstatus varchar(1000), @sqlagentuptime varchar(1000) ");
            sql.Append("SET @today = GETDATE() ");
            sql.Append("SELECT @crdate=crdate FROM sysdatabases WHERE name='tempdb' ");
            sql.Append("SET @min = DATEDIFF (mi,@crdate,@today) ");
            sql.Append("SET @days= @min/1440 ");
            sql.Append("SET @hr = (@min/60) - (@days * 24) ");
            sql.Append("SET @min= @min - ( (@hr + (@days*24)) * 60) ");
            sql.Append("select @sqlagentuptime =  ");
            sql.Append(" ltrim(str(@days)) + 'd ' ");
            sql.Append("+ ltrim(str(@hr)) +'h ' ");
            sql.Append("+ ltrim(str(@min)) +'m' ");
            sql.Append("IF NOT EXISTS ");
            sql.Append("(SELECT 1 FROM master.dbo.sysprocesses (nolock) ");
            sql.Append("	WHERE program_name = 'SQLAgent - Generic Refresher'  ");
            sql.Append("	OR program_name = 'SQLAgent - Email Logger' ");
            sql.Append("	OR program_name = 'SQLAgent - ALert Engine' ");
            sql.Append("	OR program_name = 'SQLAgent - Job invocation engine' ");


            //add program names from config if present
            foreach (var programName in _programNames)
            {
                sql.Append("	OR program_name = '" + programName.Value + "'");
            }

            sql.Append("	) ");
            sql.Append("BEGIN ");
            sql.Append("       select @sqlagentstatus = 'Not Running.' ");
            sql.Append("END ");
            sql.Append("ELSE ");
            sql.Append("BEGIN ");
            sql.Append("       select @sqlagentstatus = 'Running.' ");
            sql.Append("END ");
            sql.Append("select @sqlagentuptime sqlinstanceuptime, @sqlagentstatus sqlagentstatus ");

            using (var conn = new SqlConnection(_connectionString))
            {
                var result = SqlHelper.GetDataReader(conn, sql.ToString());


                if (result.FieldCount > 0)
                {
                    while (result.Read())
                    {
                        var sqlInstanceUptime = result["sqlinstanceuptime"].ToString();
                        var sqlAgentStatus = result["sqlagentstatus"].ToString();

                        resultCode = "0";
                        resultMessage = "Instance status and uptime returned.";

                        BuildExecuteOutput(sqlInstanceUptime, sqlAgentStatus, resultCode, resultMessage);
                    }
                }
                else
                {
                    resultMessage = "Instance status not returned.";
                    BuildExecuteOutput("n/a", "n/a", resultCode, resultMessage);
                }
            }
        }

        private void BuildExecuteOutput(string sqlInstanceUptime, string sqlAgentStatus, string resultCode, string resultMessage)
        {
            var xml = new XElement("SqlAgentStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                //new XAttribute("name", _databaseName),
                                   new XAttribute("sqlAgentStatus", sqlAgentStatus),
                                   new XAttribute("sqlInstanceUptime", sqlInstanceUptime));

            _output = xml.ToString();
        }
    }
}
