using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.Plugin.SqlServer2000.Cluster;
using Datavail.Delta.Agent.SharedCode;
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
                    if (_databaseServerInfo == null)
                        _databaseServerInfo = new SqlServerInfo(_connectionString);

                    GetBlocking();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No blocking information collected.");
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
        }

        private void GetBlocking()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = "exec SP_Who2";
            var result = _sqlRunner.RunSql(_connectionString, sql);

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var blockedTime = result["Blocked Time"].ToString();
                    var blockingSpid = result["Blocking SPID"].ToString();
                    var blockingSql = result["Blocking SQL"].ToString();
                    var blockedSpid = result["Blocked SPID"].ToString();
                    var blockedSql = result["Blocked SQL"].ToString();

                    resultCode = "0";
                    resultMessage = "Blocking information returned for metricinstance " + _metricInstance;

                    BuildExecuteOutput(blockedTime, blockingSpid, blockingSql, blockedSpid, blockedSql, resultCode, resultMessage);
                }
            }
            else
            {
                resultMessage = "No blocking information found for: " + _metricInstance;
                BuildExecuteOutput("n/a", "n/a", "n/a", "n/a", "n/a", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string blockedTime, string blockingSpid, string blockingSql, string blockedSpid, string blockedSql, string resultCode, string resultMessage)
        {
            var xml = new XElement("DatabaseServerBlockingPluginOutput",
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
                                   new XAttribute("blockedTime", blockedTime),
                                   new XAttribute("blockingSpid", blockingSpid),
                                   new XAttribute("blockingSql", blockingSql),
                                   new XAttribute("blockedSpid", blockedSpid),
                                   new XAttribute("blockedSql", blockedSql));

            _output = xml.ToString();
        }
    }
}
