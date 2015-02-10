using System;
using System.Text;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.SqlServer2008.Infrastructure;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;


namespace Datavail.Delta.Agent.Plugin.SqlServer2008
{
    public class DatabaseServerBlockingPlugin : IPlugin
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


        public DatabaseServerBlockingPlugin()
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

        public DatabaseServerBlockingPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseServerBlocking.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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
                    GetBlocking();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No blocking has been found.");
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
            _instanceName = xmlData.Attribute("InstanceName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }

            if (_databaseServerInfo == null)
                _databaseServerInfo = new SqlServerInfo(_connectionString);
        }

        private void GetBlocking()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            StringBuilder sbSql = new StringBuilder();

            sbSql.Append("DECLARE @Sec int ");
            sbSql.Append("DECLARE @NewLineChar AS varCHAR(10) ");
            sbSql.Append("SET @Sec = 1 ");
            sbSql.Append("select @NewLineChar = CHAR(13) + CHAR(10)     ");

            sbSql.Append("CREATE table #CheckBlockerDbcc (EventType varchar(255), Parameters varchar(255), EventInfo varchar(255))   ");

            sbSql.Append("IF EXISTS ( ");
            sbSql.Append("select * from tempdb.dbo.sysobjects o (NOLOCK)");
            sbSql.Append("where o.xtype in ('U')  ");
            sbSql.Append("and o.id = object_id(N'tempdb..#BlockingTable') ");
            sbSql.Append(") ");
            sbSql.Append("BEGIN ");
            sbSql.Append("DROP TABLE #BlockingTable;  ");
            sbSql.Append("END ");

            sbSql.Append("CREATE table #BlockingTable (db varchar(255), request_session_id varchar(255), request_session_command varchar(255), waiting_duration_sec varchar(255), blocking_id varchar(255), blocking_command varchar(512)) ");
            sbSql.Append("declare @resource_database_id int,         ");
            sbSql.Append(" @DBName varchar(50),     ");
            sbSql.Append(" @request_session_id int, @request_command varchar(512),   ");
            sbSql.Append(" @blocking_session_id int, @blocking_command varchar(512),   ");
            sbSql.Append(" @wait_duration_sec int,   ");
            sbSql.Append(" @TempString varchar(2000) ");

            sbSql.Append("DECLARE BlockingCr CURSOR FAST_FORWARD FOR    ");
            sbSql.Append("SELECT  ");
            sbSql.Append("    t1.resource_database_id,  ");
            sbSql.Append("    t1.request_session_id,   ");
            sbSql.Append("    t2.blocking_session_id,  ");
            sbSql.Append("	t2.wait_duration_ms, ");
            sbSql.Append("	(SELECT substring(text, s1.stmt_start/2, ");
            sbSql.Append("				1 + ((case when s1.stmt_end = -1 then ");
            sbSql.Append("				(len(convert(nvarchar(max), text)) * 2) ");
            sbSql.Append("				else ");
            sbSql.Append("					s1.stmt_end ");
            sbSql.Append("				end) - s1.stmt_start) / 2)  ");
            sbSql.Append("		 FROM sys.dm_exec_sql_text(s1.sql_handle)) Request_Command ");
            sbSql.Append("	, ");
            sbSql.Append("	(SELECT substring(text, s2.stmt_start/2, ");
            sbSql.Append("				1 + ((case when s2.stmt_end in (0, -1) then ");
            sbSql.Append("datalength(text) ");
            sbSql.Append("				else ");
            sbSql.Append("					s2.stmt_end ");
            sbSql.Append("				end) - s2.stmt_start) / 2)  ");
            sbSql.Append("		 FROM sys.dm_exec_sql_text(s2.sql_handle)) Blocking_command  ");

            sbSql.Append("FROM sys.dm_tran_locks as t1 (NOLOCK)         ");
            sbSql.Append("INNER JOIN sys.dm_os_waiting_tasks as t2 (NOLOCK) ON t1.lock_owner_address = t2.resource_address         ");
            sbSql.Append("INNER JOIN master..sysprocesses s1 (NOLOCK) ON s1.SPID = t1.request_session_id ");
            sbSql.Append("INNER JOIN master..sysprocesses s2 (NOLOCK) ON s2.SPID = t2.blocking_session_id ");
            sbSql.Append("where wait_duration_ms > (@sec * 1000)         ");
            sbSql.Append("and db_name(t1.resource_database_id) not in ('distribution')         ");
            sbSql.Append("and t1.request_session_id > 50         ");
            sbSql.Append("order by wait_duration_ms desc;      ");

            sbSql.Append("OPEN BlockingCr         ");
            sbSql.Append("Fetch_para:         ");
            sbSql.Append("fetch next from BlockingCr into @resource_database_id, @request_session_id,         ");
            sbSql.Append("    @blocking_session_id, @wait_duration_sec ");
            sbSql.Append("	,@request_command, @blocking_command          ");
            sbSql.Append("while @@fetch_status = 0         ");
            sbSql.Append("begin         ");

            sbSql.Append(" set @wait_duration_sec = (@wait_duration_sec * 1.00) / 1000         ");

            sbSql.Append("insert into #BlockingTable (db, request_session_id, request_session_command, waiting_duration_sec, blocking_id, blocking_command) ");
            sbSql.Append("values (db_name(@resource_database_id), SUBSTRING(ltrim(rtrim(isnull(@request_command, ''))), 1, 500), SUBSTRING(ltrim(rtrim(isnull(@request_command, ''))), 1, 500), ");
            sbSql.Append("SUBSTRING(ltrim(str(@wait_duration_sec)), 1, 250), SUBSTRING(ltrim(str(@blocking_session_id)), 1, 250),SUBSTRING(ltrim(rtrim(isnull(@blocking_command, ''))), 1, 500) ) ");
            sbSql.Append(" goto Fetch_para        ");
            sbSql.Append("end         ");
            sbSql.Append("close BlockingCr         ");
            sbSql.Append("deallocate BlockingCr        ");
            sbSql.Append("drop table #CheckBlockerDbcc ");
            sbSql.Append("select * from  #BlockingTable ");

            var result = _sqlRunner.RunSql(_connectionString, sbSql.ToString());
            var hasRows = false;
            var xml = BuildExecuteOutput();

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    hasRows = true;

                    var db = result["db"].ToString();
                    var requestSessionId = result["request_session_id"].ToString();
                    var requestSessionCommand = result["request_session_command"].ToString();
                    var waitingDurationSec = result["waiting_duration_sec"].ToString();
                    var blockingId = result["blocking_id"].ToString();
                    var blockingCommand = result["blocking_command"].ToString();

                    resultCode = "0";
                    resultMessage = "Blocking information returned for metricinstance " + _metricInstance;

                    xml.Root.Add(BuildExecuteOutputNode(db, requestSessionId, requestSessionCommand, waitingDurationSec, blockingId, blockingCommand, resultCode, resultMessage));
                }
            }

            if (hasRows)
            {
                _output = xml.ToString();
            }

        }

        private XElement BuildExecuteOutputNode(string db, string requestSessionId, string requestSessionCommand, string waitingDurationSec,
            string blockingId, string blockingCommand, string resultCode, string resultMessage)
        {
            var xelement = new XElement("BlockingStatus",
                                new XAttribute("resultCode", resultCode),
                                new XAttribute("resultMessage", resultMessage),
                                new XAttribute("instanceName", _instanceName),
                                new XAttribute("database", db),
                                new XAttribute("requestSessionId", requestSessionId),
                                new XAttribute("requestSessionCommand", requestSessionCommand),
                                new XAttribute("waitingDurationSec", waitingDurationSec),
                                new XAttribute("blockingId", blockingId),
                                new XAttribute("blockingCommand", blockingCommand));



            return xelement;
        }

        private XDocument BuildExecuteOutput()
        {
            var xml = new XDocument(
                            new XElement("DatabaseServerBlockingPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label)));




            return xml;
        }
    }
}
