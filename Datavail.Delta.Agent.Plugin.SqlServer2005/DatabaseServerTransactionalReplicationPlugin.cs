using System;
using System.Data.SqlClient;
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
    public class DatabaseServerTransactionalReplicationPlugin : IPlugin
    {
        private readonly IClusterInfo _clusterInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private readonly ISqlRunner _sqlRunner;
        private readonly ISqlRunner _sqlRunnerPublishers;
        private IDatabaseServerInfo _databaseServerInfo;

        private Guid _metricInstance;
        private string _label;
        private string _output;

        //Specific
        private string _connectionString;
        private string _distributionDatabaseName;
        private string _clusterGroupName;
        private string _instanceName;
        private bool _runningOnCluster = false;



        public DatabaseServerTransactionalReplicationPlugin()
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
            _sqlRunnerPublishers = new SqlServerRunner();
            _logger = new DeltaLogger();
        }

        public DatabaseServerTransactionalReplicationPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, ISqlRunner sqlRunnerPublishers, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _sqlRunnerPublishers = sqlRunnerPublishers;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseServerReplication.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

                    GetTransactionalReplicationStatus();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No Transactional Replication data found.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
                try
                {
                    _output = _logger.BuildErrorOutput("SqlServer2005.DatabaseServerTransactionalReplicationPlugin", "Execute", _metricInstance, ex.ToString());
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
            _instanceName = xmlData.Attribute("InstanceName").Value;
            _distributionDatabaseName = xmlData.Attribute("DistributionDatabaseName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void GetTransactionalReplicationStatus()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("select a.srvname as Publisher, ");
            sql.Append("b.publisher_db as Publisher_DB,  ");
            sql.Append("b.publication as Publication,  ");
            sql.Append("b.publication_type ");
            sql.Append("from master.dbo.sysservers a (nolock) ");
            sql.Append("inner join " + _distributionDatabaseName + ".dbo.MSpublications b ");
            sql.Append("on a.srvid = b.publisher_id ");

            using (var conn = new SqlConnection(_connectionString))
            {
                var publishers = SqlHelper.GetDataReader(conn, sql.ToString());

                var xml = BuildExecuteOutput();
                int resultCount = 0;

                //Loop over all publishers
                while (publishers.Read())
                {
                    var publisher = publishers["publisher"].ToString();
                    var publisherDb = publishers["publisher_DB"].ToString();
                    var publication = publishers["publication"].ToString();

                    StringBuilder sbSql = new StringBuilder();

                    sbSql.Append("use " + _distributionDatabaseName + " ");
                    sbSql.Append("exec sp_MSenum_subscriptions ");
                    sbSql.Append("@publisher = N'" + publisher + "',  ");
                    sbSql.Append("@publisher_db = N'" + publisherDb + "',  ");
                    sbSql.Append("@publication = N'" + publication + "',  ");
                    sbSql.Append("@exclude_anonymous = 0 ");

                    using (var conn1 = new SqlConnection(_connectionString))
                    {
                        var result = SqlHelper.GetDataReader(conn1, sql.ToString());

                        while (result.Read())
                        {

                            var subscriber = result["subscriber"].ToString();
                            var publication2 = publication;
                            var status = result["status"].ToString();
                            var subscriberDb = result["subscriber_db"].ToString();
                            var type = result["type"].ToString();
                            var distributionAgent = result["distribution_agent"].ToString();
                            var lastAction = result["last_action"].ToString();
                            var startTime = result["start_time"].ToString();
                            var actionTime = result["action_time"].ToString();
                            var duration = result["duration"].ToString();
                            var deliveryLatency = result["delivery_latency"].ToString();
                            var deliveryTransactions = result["delivered_transactions"].ToString();
                            var deliveredCommands = result["delivered_commands"].ToString();
                            var averageCommands = result["average_commands"].ToString();
                            var errorId = result["error_id"].ToString();
                            var jobId = (result["job_id"].ToString());
                            var localJob = result["local_job"].ToString();
                            var profileId = result["profile_id"].ToString();
                            var agentId = result["agent_id"].ToString();
                            var offloadEnabled = result["offload_enabled"].ToString();
                            var offloadServer = result["offload_server"].ToString();
                            var subscriberType = result["subscriber_type"].ToString();

                            resultCode = "0";
                            resultMessage = "Transactional Replication data found for metricinstance " + _metricInstance +
                                            " with distribution database: " + _distributionDatabaseName;

                            xml.Root.Add(BuildExecuteOutputNode(subscriber, publication2, status, subscriberDb, type,
                                                                distributionAgent, lastAction, startTime,
                                                                actionTime, duration, deliveryLatency, deliveryTransactions,
                                                                deliveredCommands,
                                                                averageCommands, errorId, jobId, localJob, profileId,
                                                                agentId, offloadEnabled,
                                                                offloadServer, subscriberType, resultCode, resultMessage));

                            resultCount++;

                        }



                        if (resultCount == 0)
                        {
                            resultMessage = "Transactional Replication data not found for metricinstance " + _metricInstance +
                                            " with distribution database: " + _distributionDatabaseName;
                            xml.Root.Add(BuildExecuteOutputNode("n/a", "n/a", "n/a", "n.a", "n/a", "n/a", "n/a", "n/a",
                                                                "n/a", "n/a", "n/a",
                                                                "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a",
                                                                "n/a", "n/a", "n/a", resultCode, resultMessage));
                        }
                        conn1.Dispose();
                        conn1.Close();
                    }


                    if (resultCount > 0)
                    {
                        _output = xml.ToString();
                    }
                }
                conn.Dispose();
                conn.Close();
            }
        }

        private XElement BuildExecuteOutputNode(string subscriber, string publication, string status, string subscriberDb, string type,
            string distributionAgent, string lastAction, string startTime, string actionTime, string duration, string deliveryLatency,
            string deliveryTransactions, string deliveredCommands, string averageCommands, string errorId,
            string jobId, string localJob, string profileId, string agentId, string offloadEnabled,
            string offloadServer, string subscriberType, string resultCode, string resultMessage)
        {
            var xml = new XElement("TransactionalReplicationStatus",
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("subscriber", subscriber),
                                   new XAttribute("publication", publication),
                                   new XAttribute("status", status),
                                   new XAttribute("subscriberDb", subscriberDb),
                                   new XAttribute("type", type),
                                   new XAttribute("distributionAgent", distributionAgent),
                                   new XAttribute("lastAction", lastAction),
                                   new XAttribute("startTime", startTime),
                                   new XAttribute("actionTime", actionTime),
                                   new XAttribute("duration", duration),
                                   new XAttribute("deliveryLatency", deliveryLatency),
                                   new XAttribute("deliveryTransactions", deliveryTransactions),
                                   new XAttribute("deliveredCommands", deliveredCommands),
                                   new XAttribute("averageCommande", averageCommands),
                                   new XAttribute("errorId", errorId),
                                   new XAttribute("jobId", jobId),
                                   new XAttribute("localJob", localJob),
                                   new XAttribute("profileId", profileId),
                                   new XAttribute("agentId", agentId),
                                   new XAttribute("offloadEnabled", offloadEnabled),
                                   new XAttribute("offloadServer", offloadServer),
                                   new XAttribute("subscriberType", subscriberType));


            return xml;
        }

        private XDocument BuildExecuteOutput()
        {
            var xml = new XDocument(
                            new XElement("DatabaseServerTransactionalReplicationPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("label", _label)));

            return xml;
        }
    }
}
