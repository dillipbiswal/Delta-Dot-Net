using System;
using System.Data.SqlClient;
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

    public class DatabaseStatusPlugin : IPlugin
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
        private string _databaseName;
        private string _instanceName;
        private string _clusterGroupName;
        private bool _runningOnCluster = false;

        public DatabaseStatusPlugin()
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

        public DatabaseStatusPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseStatus.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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
                        _logger.LogDebug("No Data Queued: Database status not found for: " + _databaseName);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
                try
                {
                    _output = _logger.BuildErrorOutput("SqlServer2008.DatabaseStatusPlugin", "Execute", _metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }
            finally
            {
                _output = null;
            }
        }

        private void ParseData(string data)
        {
            var crypto = new Encryption();
            var xmlData = XElement.Parse(data);

            _connectionString = crypto.DecryptString(xmlData.Attribute("ConnectionString").Value);
            _connectionString = _connectionString + " Pooling=false;";

            _databaseName = xmlData.Attribute("DatabaseName").Value;
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

            var sql = String.Format("SELECT name, database_id, state_desc FROM sys.databases (nolock) WHERE name='{0}'",
                                    _databaseName);
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = SqlHelper.GetDataReader(conn, sql.ToString());


                // will FieldCount return a null string as a Field if SQL returns no result?
                // changed from FieldCount where it would possible return invisible fields 
                // and never hit the else for the MISSING status
                if (result.FieldCount > 0)
                {
                    while (result.Read())
                    {
                        var databaseId = (int)result["database_id"];
                        var status = result["state_desc"].ToString();

                        resultCode = "0";
                        resultMessage = "Status returned for database: " + _databaseName;
                        // adding check for excessive & superflous ONLINE status reports from agents
                        if (status != "ONLINE")
                        {
                            BuildExecuteOutput(databaseId, status, resultCode, resultMessage);
                        }
                    }
                }
                else
                {
                    resultMessage = "Status not returned for database: " + _databaseName;
                    BuildExecuteOutput(-1, "MISSING", resultCode, resultMessage);
                }
                conn.Dispose();
                conn.Close();
            }
        }

        private void BuildExecuteOutput(int databaseId, string status, string resultCode, string resultMessage)
        {
            var xml = new XElement("DatabaseStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("name", _databaseName),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("databaseId", databaseId),
                                   new XAttribute("status", status));

            _output = xml.ToString();
        }
    }
}