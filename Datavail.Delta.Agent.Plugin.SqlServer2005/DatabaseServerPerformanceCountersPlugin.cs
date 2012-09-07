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
    public class DatabaseServerPerformanceCountersPlugin : IPlugin
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

        public DatabaseServerPerformanceCountersPlugin()
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

        public DatabaseServerPerformanceCountersPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("PerformanceMetrics.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

                    GetPerformanceMetrics();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: Performance Metrics not collected.");
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

        private void GetPerformanceMetrics()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("select * from sys.dm_os_performance_counters ");
            sql.Append("where counter_name in ( ");
            sql.Append("'Page lookups/sec', 'Lock Waits/sec', 'Transactions/sec', 'Batch Requests/sec', 'Page Splits/sec', ");
            sql.Append("'Page Life Expectancy', 'SQL Compilations/sec', 'Log Flushes/sec', 'Total Server Memory (KB)', ");
            sql.Append("'Target Server Memory (KB)', 'Lazy Writes/sec', 'Checkpoint Pages/sec' ");
            sql.Append(") ");
            sql.Append("and instance_name in ('',  '_Total') ");
            sql.Append("and object_name not like ('%:Locks%') ");
            sql.Append("order by counter_name");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                var batchRequestsPerSec = string.Empty;
                var checkpointPagesPerSec = string.Empty;
                var lazyWritesPerSec = string.Empty;
                var logFlushesPerSec = string.Empty;
                var pageLifeExpectancy = string.Empty;
                var pageLookupsPerSec = string.Empty;
                var pageSplitsPerSec = string.Empty;
                var sqlCompilationsPerSec = string.Empty;
                var targetServerMemoryKb = string.Empty;
                var totalServerMemoryKb = string.Empty;
                var transactionsPerSec = string.Empty;

                while (result.Read())
                {

                    var counterName = result["counter_name"].ToString().Trim();

                    switch (counterName)
                    {
                        case "Batch Requests/sec" :
                            batchRequestsPerSec = result["cntr_value"].ToString();
                            break;
                        case "Checkpoint pages/sec":
                            checkpointPagesPerSec = result["cntr_value"].ToString();
                            break;
                        case "Lazy writes/sec":
                            lazyWritesPerSec = result["cntr_value"].ToString();
                            break;
                        case "Log Flushes/sec":
                            logFlushesPerSec = result["cntr_value"].ToString();
                            break;
                        case "Page life expectancy":
                            pageLifeExpectancy = result["cntr_value"].ToString();
                            break;
                        case "Page lookups/sec":
                            pageLookupsPerSec = result["cntr_value"].ToString();
                            break;
                        case "Page Splits/sec":
                            pageSplitsPerSec = result["cntr_value"].ToString();
                            break;
                        case "SQL Compilations/sec":
                            sqlCompilationsPerSec = result["cntr_value"].ToString();
                            break;
                        case "Target Server Memory (KB)":
                            targetServerMemoryKb = result["cntr_value"].ToString();
                            break;
                        case "Total Server Memory (KB)":
                            totalServerMemoryKb = result["cntr_value"].ToString();
                            break;
                        case "Transactions/sec":
                            transactionsPerSec = result["cntr_value"].ToString();
                            break;
                    }

                }



                resultCode = "0";
                resultMessage = "Performance metrics returned.";
                BuildExecuteOutput(batchRequestsPerSec, checkpointPagesPerSec, lazyWritesPerSec, logFlushesPerSec,
                    pageLifeExpectancy, pageLookupsPerSec, pageSplitsPerSec, sqlCompilationsPerSec, targetServerMemoryKb,
                    totalServerMemoryKb, transactionsPerSec, resultCode, resultMessage);
            }
            else
            {
                resultMessage = "No performance metrics returned.";
                BuildExecuteOutput("0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string batchRequestsPerSec, string checkpointPagesPerSec, string lazyWritesPerSec, string logFlushesPerSec,
            string pageLifeExpectancy, string pageLookupsPerSec, string pageSplitsPerSec, string sqlCompilationsPerSec,
            string targetServerMemoryKB, string totalServerMemoryKB, string transactionsPerSec, string resultCode, string resultMessage)
        {
            var xml = new XElement("DatabaseServerPerformanceCountersPluginOutput",
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                //   new XAttribute("name", _databaseName),
                                   new XAttribute("batchRequestsPerSec", batchRequestsPerSec),
                                   new XAttribute("checkpointPagesPerSec", checkpointPagesPerSec),
                                   new XAttribute("lazyWritesPerSec", lazyWritesPerSec),
                                   new XAttribute("logFlushesPerSec", logFlushesPerSec),
                                   new XAttribute("pageLifeExpectancy", pageLifeExpectancy),
                                   new XAttribute("pageLookupsPerSec", pageLookupsPerSec),
                                   new XAttribute("pageSplitsPerSec", pageSplitsPerSec),
                                   new XAttribute("sqlCompilationsPerSec", sqlCompilationsPerSec),
                                   new XAttribute("targetServerMemoryKb", targetServerMemoryKB),
                                   new XAttribute("totalServerMemoryKb", totalServerMemoryKB),
                                   new XAttribute("transactionsPerSec", transactionsPerSec));

            _output = xml.ToString();
        }


    }
}
