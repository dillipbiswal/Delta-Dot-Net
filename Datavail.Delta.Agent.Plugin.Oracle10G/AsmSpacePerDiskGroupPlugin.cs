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
    public class AsmSpacePerDiskGroupPlugin : IPlugin   
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


        public AsmSpacePerDiskGroupPlugin()
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

        public AsmSpacePerDiskGroupPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("AllDatabaseParametersPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetAsmSpacePerDiskGroup();
                
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


        private void GetAsmSpacePerDiskGroup()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

            sql.Append("select d.GROUP_NUMBER, dg.name as GROUP_NAME, d.DISK_NUMBER, d.TOTAL_MB, d.FREE_MB, d.name as NAME, d.PATH ");
            sql.Append("from v$asm_disk d, v$asm_diskgroup dg where d.group_number = dg.group_number "); 
		  	sql.Append("order by 2,3");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var groupNumber = result["GROUP_NUMBER"].ToString();
                    var groupName = result["GROUP_NAME"].ToString();
                    var diskNumber = result["DISK_NUMBER"].ToString();
                    var totalMb = result["TOTAL_MB"].ToString();
                    var freeMb = result["FREE_MB"].ToString();
                    var name = result["NAME"].ToString();
                    var path = result["PATH"].ToString();

                    resultCode = "0";
                    resultMessage = "Status returned for Asm space per disk group: " + _instanceName;

                    BuildExecuteOutput(groupNumber, groupName, diskNumber, totalMb, freeMb, name, path, resultCode, resultMessage);
                }
                
            }
            else
            {
                resultMessage = "Asm space per disk group not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string groupNumber, string groupName, string diskNumber, string totalMb, string freeMb, string name, string path, string resultCode, string resultMessage)
        {
            var xml = new XElement("AsmSpacePerDiskGroupPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("groupNumber", groupNumber),
                                   new XAttribute("groupName", groupName),
                                   new XAttribute("diskNumber", diskNumber),
                                   new XAttribute("totalMb", totalMb),
                                   new XAttribute("freeMb", freeMb),
                                   new XAttribute("name", name),
                                   new XAttribute("path", path)
                                   );


            _output = xml.ToString();
        }
    }
}
