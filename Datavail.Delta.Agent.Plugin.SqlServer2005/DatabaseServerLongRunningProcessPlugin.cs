using System;
using System.Text;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.SqlServer2005.Cluster;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;


namespace Datavail.Delta.Agent.Plugin.SqlServer2005
{
	public class DatabaseServerLongRunningProcessPlugin : IPlugin
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
		private string _threshold;
		private string _clusterGroupName;
		private string _instanceName;
		private bool _runningOnCluster = false;

		public DatabaseServerLongRunningProcessPlugin()
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

		public DatabaseServerLongRunningProcessPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
		{
			_clusterInfo = clusterInfo;
			_dataQueuer = dataQueuer;
			_sqlRunner = sqlRunner;
			_databaseServerInfo = databaseServerInfo;
			_logger = logger;
		}

		public void Execute(Guid metricInstance, string label, string data)
		{
			_logger.LogDebug(String.Format("DatabaseServerLongRunningProcess.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

					GetLongRunningProcess();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No long running processes detected.");
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
			_threshold = xmlData.Attribute("Threshold").Value;
			_instanceName = xmlData.Attribute("InstanceName").Value;
										  
			if (xmlData.Attribute("ClusterGroupName") != null)
			{
				_runningOnCluster = true;
				_clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
			}
		}

		private void GetLongRunningProcess()
		{
			var resultCode = "-1";
			var resultMessage = string.Empty;
			var sql = new StringBuilder();

			sql.Append("SET NOCOUNT ON ");  
			sql.Append("DECLARE @Threshold INT SET  @Threshold =  " + _threshold);  
			sql.Append("SELECT	  ");  
			sql.Append("@Threshold [Long Process Threshold],  ");  
			sql.Append("DATEDIFF(mi, last_batch,   ");  
			sql.Append("getdate()) [Current Run Time] ,  ");  
			sql.Append("spid [Session ID],  ");  
			sql.Append("program_name [Program],  ");  
			sql.Append("last_batch [Last Batch],  ");
            sql.Append("st.text [SQL]   ");
            sql.Append("FROM   ");
            sql.Append("master.dbo.sysprocesses s   ");
            sql.Append("cross apply sys.dm_exec_sql_text(sql_handle) st "); 
			sql.Append("WHERE status in ('running', 'rollback', 'pending', 'runnable', 'suspended')  ");
			sql.Append("AND spid > 50 AND DATEDIFF(mi, last_batch, getdate()) > " + _threshold );  

			var result = _sqlRunner.RunSql(_connectionString, sql.ToString());
		    var hasRows = false;
		    var xml = BuildExecuteOutput();

			while (result.Read())
			{
			    hasRows = true;

			    var longProcessThreshold = _threshold;
				var currentRunTime = result["Current Run Time"].ToString();
				var spid = result["Session ID"].ToString();
				var programName = result["Program"].ToString();
				var lastBatch = result["Last Batch"].ToString();
				var sqlStatements = result["SQL"].ToString();

				resultCode = "0";
				resultMessage = "Successfully retrieved Long running processes with threshold over: " + _threshold;
				xml.Root.Add(BuildExecuteOutputNode(
                                longProcessThreshold, currentRunTime, spid, programName, lastBatch, sqlStatements, resultCode, resultMessage));

			}
			
			if (hasRows)
			{
                _output = xml.ToString();
			}
		}

        private XElement BuildExecuteOutputNode(string longProcessThreshold, string currentRunTime, string spid, string programName, string lastBatch, string sqlStatements, string resultCode, string resultMessage)
		{
			var xelement = new XElement("LongRunningProcessResult",
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
                                   new XAttribute("longProcessThreshold", longProcessThreshold),
								   new XAttribute("currentRunTime", currentRunTime),
								   new XAttribute("spid", spid),
								   new XAttribute("programName", programName),
								   new XAttribute("lastBatch", lastBatch),
								   new XAttribute("sqlStatements", sqlStatements));

            return xelement;
		}

        private XDocument BuildExecuteOutput()
        {
            var xml = new XDocument(
                new XElement("DatabaseServerLongRunningProcessPluginOutput",
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
