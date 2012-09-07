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
    public class GatherStatsParametersPlugin : IPlugin   
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


        public GatherStatsParametersPlugin()
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

        public GatherStatsParametersPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("GatherStatsParametersPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetGatherStatsParameters();
                
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


        private void GetGatherStatsParameters()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("SELECT 'CASCADE' \"Parameter\", dbms_stats.get_param('CASCADE') \"Value\" FROM DUAL ");
            sql.AppendLine("UNION ");
            sql.AppendLine("SELECT 'DEGREE', dbms_stats.get_param('DEGREE') FROM DUAL ");
            sql.AppendLine("UNION ");
            sql.AppendLine("SELECT 'ESTIMATE PERCENT',dbms_stats.get_param('ESTIMATE_PERCENT') FROM DUAL ");
            sql.AppendLine("UNION ");
            sql.AppendLine("SELECT 'METHOD OPT', dbms_stats.get_param('METHOD_OPT') FROM DUAL ");
            sql.AppendLine("UNION ");
            sql.AppendLine("SELECT 'NO INVALIDATE', dbms_stats.get_param('NO_INVALIDATE') FROM DUAL ");
            sql.AppendLine("UNION ");
            sql.AppendLine("SELECT 'GRANULARITY', dbms_stats.get_param('GRANULARITY') FROM DUAL ");
            sql.AppendLine("UNION ");
            sql.AppendLine("SELECT 'AUTOSTATS_TARGET', dbms_stats.get_param('AUTOSTATS_TARGET') FROM DUAL; ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var parameter = result["Parameter"].ToString();
                    var value = result["Value"].ToString();

                    resultCode = "0";
                    resultMessage = "Gather Stats Parameters returned: " + _instanceName;

                    BuildExecuteOutput(parameter, value, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Gather Stats Parameters not returned: " + _instanceName;
                BuildExecuteOutput("", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string parameter, string value, string resultCode, string resultMessage)
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
                                   new XAttribute("parameter", parameter),
                                   new XAttribute("value", value)
                );

            _output = xml.ToString();
        }
    }
}
