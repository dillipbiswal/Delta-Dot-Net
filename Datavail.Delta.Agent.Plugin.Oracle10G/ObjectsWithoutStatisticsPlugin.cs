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
    public class ObjectsWithoutStatisticsPlugin : IPlugin   
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


        public ObjectsWithoutStatisticsPlugin()
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

        public ObjectsWithoutStatisticsPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("ObjectsWithoutStatisticsPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetObjectsWithoutStatistics();
                
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


        private void GetObjectsWithoutStatistics()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.AppendLine("select 'Table' \"Object Type\", ");
            sql.AppendLine("owner \"Schema\", ");
            sql.AppendLine("table_name \"Name\", ");
            sql.AppendLine("NULL \"Partition Name\" ");
            sql.AppendLine("from   sys.dba_tables ");
            sql.AppendLine("where  last_analyzed is null ");
            sql.AppendLine("and    owner not in ('SYS','SYSTEM') ");
            sql.AppendLine("and    partitioned = 'NO' ");
            sql.AppendLine("union ");
            sql.AppendLine("select 'Index' \"Object Type\", ");
            sql.AppendLine("owner \"Schema\", ");
            sql.AppendLine("index_name \"Name\", ");
            sql.AppendLine("NULL \"Partition Name\" ");
            sql.AppendLine("from   sys.dba_indexes ");
            sql.AppendLine("where  last_analyzed is null ");
            sql.AppendLine("and    owner not in ('SYS','SYSTEM') ");
            sql.AppendLine("and    partitioned='NO' ");
            sql.AppendLine("and    index_type != 'LOB' ");
            sql.AppendLine("and    temporary != 'Y' ");
            sql.AppendLine("union  select 'Table Partition' \"Object Type\", ");
            sql.AppendLine("table_owner \"Schema\", ");
            sql.AppendLine("table_name \"Name\", ");
            sql.AppendLine("partition_name \"Partition Name\" ");
            sql.AppendLine("from   sys.dba_tab_partitions ");
            sql.AppendLine("where  last_analyzed is null ");
            sql.AppendLine("and    table_owner not in ('SYS','SYSTEM') ");
            sql.AppendLine("union ");
            sql.AppendLine("select 'Index Partition' \"Object Type\", ");
            sql.AppendLine("index_owner \"Schema\", ");
            sql.AppendLine("index_name \"Name\", ");
            sql.AppendLine("partition_name \"Partition Name\" ");
            sql.AppendLine("from   sys.dba_ind_partitions ");
            sql.AppendLine("where  last_analyzed is null ");
            sql.AppendLine("and    index_owner not in ('SYS','SYSTEM') ");
            sql.AppendLine("order by \"Schema\", \"Object Type\", \"Name\"; ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var objectType = result["Object Type"].ToString();
                    var schema = result["Schema"].ToString();
                    var name = result["Name"].ToString();
                    var partitionName = result["Partition Name"].ToString();
   
    
                    resultCode = "0";
                    resultMessage = "Objects Without Statistics returned: " + _instanceName;

                    BuildExecuteOutput(objectType, schema, name, partitionName, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Objects Without Statistics not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string objectType, string schema, string name, string partitionName, string resultCode, string resultMessage)
        {
            var xml = new XElement("ObjectsWithoutStatisticsPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("objectType", objectType),
                                   new XAttribute("schema", schema),
                                   new XAttribute("name", name),
                                   new XAttribute("partitionName", partitionName)
                                   );

            _output = xml.ToString();
        }
    }
}
