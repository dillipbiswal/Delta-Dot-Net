using System;
using System.Collections.Generic;
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
    public class DatabaseServerMergeReplicationPlugin : IPlugin
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
        private bool _runningOnCluster = false;



        public DatabaseServerMergeReplicationPlugin()
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

        public DatabaseServerMergeReplicationPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, ISqlRunner sqlRunnerPublishers, IDatabaseServerInfo databaseServerInfo)
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
            _logger.LogDebug(String.Format("DatabaseServerMergeReplication.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

                    GetMergeReplicationStatus();
                    
                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No Merge Replication data found.");
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
            _distributionDatabaseName = xmlData.Attribute("DistributionDatabaseName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void GetMergeReplicationStatus()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("select a.srvname as Publisher, ");
            sql.Append("b.publisher_db as Publisher_DB,  ");
            sql.Append("b.publication as Publication,  ");
            sql.Append("b.publication_type ");
            sql.Append("from master.dbo.sysservers a ");
            sql.Append("inner join " + _distributionDatabaseName + ".dbo.MSpublications b ");
            sql.Append("on a.srvid = b.publisher_id ");

            var publishers = _sqlRunnerPublishers.RunSql(_connectionString, sql.ToString());
            var xml = BuildExecuteOutput();
            var resultCount = 0;

            //Loop over all publishers
            while (publishers.Read())
            {
                var publisher = publishers["publisher"].ToString();
                var publisherDd = publishers["publisher_DB"].ToString();
                var publication = publishers["publication"].ToString();
                var publicationType = publishers["publication_type"].ToString();

                StringBuilder sbSql = new StringBuilder();

                sbSql.Append("use " + _distributionDatabaseName + " ");
                sbSql.Append("exec sp_MSenum_merge_subscriptions ");
                sbSql.Append("@publisher = N'" + publisher + "',  ");
                sbSql.Append("@publisher_db = N'" + publisherDd + "',  ");
                sbSql.Append("@publication = N'" + publication + "',  ");
                sbSql.Append("@exclude_anonymous = 0 ");

                var result = _sqlRunner.RunSql(_connectionString, sbSql.ToString());
                var xelements = new List<XElement>();

                while (result.Read())
                {
                    var publication2 = publication;
                    var subscriber = result["subscriber"].ToString();
                    var status = result["status"].ToString();
                    var subscriberDb = result["subscriber_db"].ToString();
                    var type = result["type"].ToString();
                    var agentName = result["agent_name"].ToString();
                    var lastAction = result["last_action"].ToString();
                    var actionTime = result["action_time"].ToString();
                    var startTime = result["start_time"].ToString();
                    var duration = result["duration"].ToString();
                    var deliveryRate = result["delivery_rate"].ToString();
                    var downloadInserts = result["download_inserts"].ToString();
                    var downloadUpdates = result["download_updates"].ToString();
                    var downloadDeletes = result["download_deletes"].ToString();
                    var publisherConflicts = result["publisher_conficts"].ToString();
                    var uploadInserts = result["upload_inserts"].ToString();
                    var uploadUpdates = result["upload_updates"].ToString();
                    var uploadDeletes = result["upload_deletes"].ToString();
                    var subscriberConflicts = result["subscriber_conficts"].ToString();
                    var errorId = result["error_id"].ToString();
                    var jobId = result["job_id"].ToString();
                    var localJob = result["local_job"].ToString();
                    var profileId = result["profile_id"].ToString();
                    var agentId = result["agent_id"].ToString();
                    var lastTimestamp = result["last_timestamp"].ToString();
                    var offloadEnabled = result["offload_enabled"].ToString();
                    var offloadServer = result["offload_server"].ToString();
                    var subscriberType = result["subscriber_type"].ToString();

                    resultCode = "0";
                    resultMessage = "Merge Replication data found for metricinstance " + _metricInstance +
                                    " with distribution database: " + _distributionDatabaseName;

                    xml.Root.Add(BuildExecuteOutputNode(subscriber,
                                                        publication2,
                                                        status,
                                                        subscriberDb,
                                                        type,
                                                        agentName,
                                                        lastAction,
                                                        actionTime,
                                                        startTime,
                                                        duration,
                                                        deliveryRate,
                                                        downloadInserts,
                                                        downloadUpdates,
                                                        downloadDeletes,
                                                        publisherConflicts,
                                                        uploadInserts,
                                                        uploadUpdates,
                                                        uploadDeletes,
                                                        subscriberConflicts,
                                                        errorId,
                                                        jobId,
                                                        localJob,
                                                        profileId,
                                                        agentId,
                                                        lastTimestamp,
                                                        offloadEnabled,
                                                        offloadServer,
                                                        subscriberType,
                                                        resultCode,
                                                        resultMessage));

                    resultCount++;
                }


                if (resultCount == 0)
                {
                    resultMessage = "Merge Replication data not found for metricinstance " + _metricInstance +
                                    " with distribution database: " + _distributionDatabaseName;
                    xml.Root.Add(BuildExecuteOutputNode("n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a",
                        "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", "n/a",
                        "n/a", "n/a", "n/a", "n/a", "n/a", "n/a", resultCode, resultMessage));
                }
            }

            if (resultCount > 0)
            {
                _output = xml.ToString();
            }

        }

        private XElement BuildExecuteOutputNode(string subscriber, string publication, string status, string subscriberDb, string type,
            string agentName, string lastAction, string actionTime, string startTime, string duration, string deliveryRate,
            string downloadInserts, string downloadUpdates, string downloadDeletes, string publisherConflicts,
            string uploadInserts, string uploadUpdates, string uploadDeletes, string subscriberConflicts,
            string errorId, string jobId, string localJob, string profileId, string agentId, string lastTimestamp,
            string offloadEnabled, string offloadServer, string subscriberType, string resultCode, string resultMessage)
        {
            var xelement = new XElement("MergeReplicationStatus",
                                new XAttribute("resultCode", resultCode),
                                new XAttribute("resultMessage", resultMessage),
                                new XAttribute("publication", publication),
                                new XAttribute("subscriber", subscriber),
                                new XAttribute("status", status),
                                new XAttribute("subscriberDb", subscriberDb),
                                new XAttribute("type", type),
                                new XAttribute("agentName", agentName),
                                new XAttribute("lastAction", lastAction),
                                new XAttribute("actionTime", actionTime),
                                new XAttribute("startTime", startTime),
                                new XAttribute("duration", duration),
                                new XAttribute("deliveryRate", deliveryRate),
                                new XAttribute("downloadInserts", downloadInserts),
                                new XAttribute("downloadUpdates", downloadUpdates),
                                new XAttribute("downloadDeletes", downloadDeletes),
                                new XAttribute("publisherConflicts", publisherConflicts),
                                new XAttribute("uploadInserts", uploadInserts),
                                new XAttribute("uploadUpdates", uploadUpdates),
                                new XAttribute("uploadDeletes", uploadDeletes),
                                new XAttribute("subscriberConflicts", subscriberConflicts),
                                new XAttribute("errorId", errorId),
                                new XAttribute("jobId", jobId),
                                new XAttribute("localJob", localJob),
                                new XAttribute("profileId", profileId),
                                new XAttribute("agentId", agentId),
                                new XAttribute("lastTimestamp", lastTimestamp),
                                new XAttribute("offloadEnabled", offloadEnabled),
                                new XAttribute("offloadServer", offloadServer),
                                new XAttribute("subscriberType", subscriberType));

            return xelement;
        }



        private XDocument BuildExecuteOutput()
        {
            var xml = new XDocument(
                            new XElement("DatabaseServerMergeReplicationPluginOutput",
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
