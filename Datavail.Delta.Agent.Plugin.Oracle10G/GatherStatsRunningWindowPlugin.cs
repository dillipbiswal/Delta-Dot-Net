using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using System.Text;
using Datavail.Delta.Infrastructure.Agent.ServerInfo;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;

namespace Datavail.Delta.Agent.Plugin.Oracle10g
{
    public class GatherStatsRunningWindowPlugin : IPlugin   
    {
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        private readonly ISqlRunner _sqlRunner;
        private IDatabaseServerInfo _oracleServerInfo;

        private Guid _metricInstance;
        private string _label;
        private string _output;

        //Specific
        private string _connectionString;
        private string _instanceName;


        public GatherStatsRunningWindowPlugin()
        {
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

        public GatherStatsRunningWindowPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("GatherStatsRunningWindowPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetGatherStatsRunningWindow();
                
                _dataQueuer.Queue(_output);
                _logger.LogDebug("Data Queued: " + _output);
                
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


            if (_oracleServerInfo == null)
                _oracleServerInfo = new OracleServerInfo(_connectionString);
        }


        private void GetGatherStatsRunningWindow()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("SELECT a.job_name, ");
            sql.AppendLine("a.enabled       , as Running ");
            sql.AppendLine("c.window_name   , ");
            sql.AppendLine("c.schedule_name , ");
            sql.AppendLine("c.start_date    , ");
            sql.AppendLine(" c.repeat_interval ");
            sql.AppendLine("FROM dba_scheduler_jobs a      , ");
            sql.AppendLine("dba_scheduler_wingroup_members b, ");
            sql.AppendLine("dba_scheduler_windows c ");
            sql.AppendLine(" WHERE job_name   = 'GATHER_STATS_JOB' ");
            sql.AppendLine("AND a.schedule_name=b.window_group_name ");
            sql.AppendLine("AND b.window_name  =c.window_name; ");


            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var jobName = result["JOB_NAME"].ToString();
                    var running = result["Running"].ToString();
                    var windowName = result["WINDOW_NAME"].ToString();
                    var scheduleName = result["SCHEDULE_NAME"].ToString();
                    var startDate = result["START_DATE"].ToString();
                    var repeatInterval = result["REPEAT_INTERVAL"].ToString();

                    resultCode = "0";
                    resultMessage = "Gather Stats Running Window returned: " + _instanceName;

                    BuildExecuteOutput(jobName, running, windowName, scheduleName, startDate, repeatInterval, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Gather Stats Running Window not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string jobName, string running, string windowName, string scheduleName, string startDate, string repeatInterval,
            string resultCode, string resultMessage)
        {
            var xml = new XElement("GatherStatsRunningWindowPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("running", running),
                                   new XAttribute("jobName", jobName),
                                   new XAttribute("windowName", windowName),
                                   new XAttribute("scheduleName", scheduleName),
                                   new XAttribute("startDate", startDate),
                                   new XAttribute("repeatInterval", repeatInterval)
                                   );

            _output = xml.ToString();
        }
    }
}
