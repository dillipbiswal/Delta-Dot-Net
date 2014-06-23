using System;
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
    public class DatabaseServerJobsPlugin : IPlugin
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
        private string _jobName;
        private string _instanceName;
        private string _clusterGroupName;
        private bool _runningOnCluster = false;

        public DatabaseServerJobsPlugin()
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

        public DatabaseServerJobsPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseServerJobs.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

                    GetDatabaseServerJobStatus();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No job status data colleced for job: " + _jobName);
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
            _jobName = xmlData.Attribute("JobName").Value;
            _instanceName = xmlData.Attribute("InstanceName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }
        }

        private void GetDatabaseServerJobStatus()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("USE msdb ");
            sql.Append("SELECT DISTINCT   ");
            sql.Append("replace(j.name, '''', '') as JobName,  ");
            sql.Append("j.job_id , j.description as JobDescription,  ");
            sql.Append("h.run_date as LastStatusDate,   ");
            sql.Append("h.*,   ");
            sql.Append("case h.run_status when 0 then 'Failed' when 1 then 'Successful' when 2 then 'Retry' when 3 then 'Cancelled' when 4 then 'In Progress' end as JobStatus ");
            sql.Append("FROM   ");
            sql.Append("sysjobhistory h,   ");
            sql.Append("sysjobs j   ");
            sql.Append("WHERE j.job_id = h.job_id   ");
            // changes per bug ID(s): 53, 29 & 30
            //sql.Append("AND h.run_date = (select max(hi.run_date) from sysjobhistory hi where h.job_id = hi.job_id and h.step_id = hi.step_id)   ");
            //sql.Append("AND h.run_time = (select max(hi.run_time) from sysjobhistory hi where h.job_id = hi.job_id and h.step_id = hi.step_id   ");
            //sql.Append("AND run_date = (select max(hi.run_date) from sysjobhistory hi where h.job_id = hi.job_id and h.step_id = hi.step_id) )   ");
            sql.Append("AND h.instance_id > (select isnull(max(sjh.instance_id),0) FROM sysjobhistory sjh ");
            sql.Append("WHERE sjh.job_id = h.job_id ");
            sql.Append("AND sjh.step_id = 0 ");
            sql.Append("AND sjh.instance_id not in (select max(sjh2.instance_id) FROM sysjobhistory sjh2 ");
            sql.Append("WHERE sjh2.job_id = h.job_id ");
            sql.Append("AND sjh2.step_id = 0)) ");
            // job name replace condition
            sql.Append("AND j.name = '" + _jobName.Replace("'", "''") + "' ");
            sql.Append("order by  step_id, JobName, LastStatusDate  ");



            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());
            var xml = BuildExecuteOutput();
            var hasRows = false;

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    hasRows = true;

                    var jobId = result["job_id"].ToString();
                    var jobName = result["JobName"].ToString();
                    var jobStatus = result["JobStatus"].ToString();
                    var message = result["message"].ToString();
                    var retriesAttempted = result["retries_attempted"].ToString();
                    var runDate = result["run_date"].ToString();
                    var runDuration = result["run_duration"].ToString();
                    var runTime = result["run_time"].ToString();
                    var stepId = result["step_id"].ToString();
                    var stepName = result["step_name"].ToString();

                    resultCode = "0";
                    resultMessage = _jobName + " status successfully returned.";

                    xml.Root.Add(BuildExecuteOutputNode(jobId, jobName, jobStatus, message,
                        retriesAttempted, runDate, runDuration, runTime, stepId, stepName,
                        resultCode, resultMessage));

                }
            }
            
            if (hasRows)
            {
                _output = xml.ToString();
            }
        }







        private XElement BuildExecuteOutputNode(string jobId, string jobName, string jobStatus,
            string message, string retriesAttempted, string runDate, string runDuration,
            string runTime, string stepId, string stepName, string resultCode, string resultMessage)
        {
            var xml = new XElement("JobStatus",
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("jobName", _jobName),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("jobId", jobId),
                                   new XAttribute("jobStatus", jobStatus),
                                   new XAttribute("message", message),
                                   new XAttribute("retriesAttempted", retriesAttempted),
                                   new XAttribute("runDate", runDate),
                                   new XAttribute("runDuration", runDuration),
                                   new XAttribute("runTime", runTime),
                                   new XAttribute("stepId", stepId),
                                   new XAttribute("stepName", stepName));


            return xml;
        }

        private XDocument BuildExecuteOutput()
        {
            var xml = new XDocument(
                            new XElement("DatabaseJobStatusPluginOutput",
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
