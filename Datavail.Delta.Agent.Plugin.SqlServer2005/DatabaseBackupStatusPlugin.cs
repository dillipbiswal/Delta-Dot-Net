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
    public class DatabaseBackupStatusPlugin : IPlugin
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

        public DatabaseBackupStatusPlugin()
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

        public DatabaseBackupStatusPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseBackupStatusPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

                    GetBackupStatus();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No backup status colleced for database " + _databaseName );
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
            _databaseName = xmlData.Attribute("DatabaseName").Value;
            _instanceName = xmlData.Attribute("InstanceName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void GetBackupStatus()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;
            var sql = new StringBuilder();
            sql.Append(" SELECT x.database_name, z.physical_device_name, CONVERT(char(20), x.backup_finish_date, 108) FinishTime,  ");
            sql.Append(" x.backup_finish_date, DATEDIFF(mi, x.backup_finish_date, getdate() ) as MinsSinceLast  from msdb.dbo.backupset x   ");
            sql.Append("JOIN ( SELECT a.database_name,  max(a.backup_finish_date) backup_finish_date FROM msdb.dbo.backupset a WHERE type = 'D'  ");
            sql.Append("GROUP BY a.database_name ) y on x.database_name = y.database_name  and x.backup_finish_date = y.backup_finish_date   ");
            sql.Append("JOIN msdb.dbo.backupmediafamily z (nolock) ON x.media_set_id = z.media_set_id   ");
            sql.Append("JOIN master.dbo.sysdatabases d (nolock) ON d.name = x.database_name and d.name = '" + _databaseName + "'");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.Read())
            {
                var physicalDeviceName = result["physical_device_name"].ToString();
                var backupFinishTimeStamp = DateTime.Parse(result["backup_finish_date"].ToString());
                var minsSinceLast = result["MinsSinceLast"].ToString();

                resultCode = "0";
                resultMessage = "Successfully retrieved backup history for database: " + _databaseName;
                BuildExecuteOutput(_databaseName, physicalDeviceName, backupFinishTimeStamp.ToString(), minsSinceLast, resultCode, resultMessage);
            }
            else
            {
                resultMessage = "No backup history found for database: " + _databaseName;
                BuildExecuteOutput(_databaseName, "N/A", DateTime.MinValue.ToString(), "-1", resultCode, resultMessage);
            }
        }
        
        private void BuildExecuteOutput(string databaseName, string physicalDeviceName, string backupFinishTimeStamp, string minsSinceLast, string resultCode, string resultMessage)
        {
            var xml = new XElement("DatabaseBackupStatusPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("name", databaseName),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("physicalDeviceName", physicalDeviceName),
                                   new XAttribute("backupFinishTimeStamp", backupFinishTimeStamp),
                                   new XAttribute("minsSinceLast", minsSinceLast));

            _output = xml.ToString();
        }

    }
}
