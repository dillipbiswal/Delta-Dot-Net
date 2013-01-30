﻿using System;
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
    public class InvalidObjectsPlugin : IPlugin   
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


        public InvalidObjectsPlugin()
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

        public InvalidObjectsPlugin(IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo oracleServerInfo)
        {
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _oracleServerInfo = oracleServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("InvalidObjectsPlugins.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                          metricInstance, label, data));
            try
            {
                Guard.GuidArgumentNotEmpty(metricInstance, "metricInstance");
                Guard.ArgumentNotNullOrEmptyString(label, "label");
                Guard.ArgumentNotNullOrEmptyString(data, "data");

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);
                GetInvalidObjects();
                
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


        private void GetInvalidObjects()
        {
            var resultCode = "-1";
            string resultMessage;

            var sql = new StringBuilder();

        sql.AppendLine("select owner \"Schema\", ");
        sql.AppendLine("object_name \"Name\", ");
        sql.AppendLine("object_id \"Id #\", ");
        sql.AppendLine("object_type \"Type\", ");
        sql.AppendLine("status \"Status\" ");
        sql.AppendLine("from   dba_objects ");
        sql.AppendLine("where  status != 'VALID' ");
        sql.AppendLine("order by owner, object_type; ");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            if (result.FieldCount > 0)
            {
                while (result.Read())
                {
                    var schema = result["Schema"].ToString();
                    var name = result["Name"].ToString();
                    var id = result["Id #"].ToString();
                    var type = result["Type"].ToString();
                    var status = result["Status"].ToString();
   
                    resultCode = "0";
                    resultMessage = "Invalid Objects returned: " + _instanceName;

                    BuildExecuteOutput(schema, name, id, type, status, resultCode, resultMessage);
                }

            }
            else
            {
                resultMessage = "Invalid Objects not returned: " + _instanceName;
                BuildExecuteOutput("", "", "", "", "", resultCode, resultMessage);
            }
        }

        private void BuildExecuteOutput(string schema, string name, string id, string type, string status, string resultCode, string resultMessage)
        {
            var xml = new XElement("InvalidObjectsPluginsOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("product", _oracleServerInfo.Product),
                                   new XAttribute("productVersion", _oracleServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _oracleServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _oracleServerInfo.ProductEdition),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("schema", schema),
                                   new XAttribute("name", name),
                                   new XAttribute("id", id),
                                   new XAttribute("type", type),
                                   new XAttribute("status", status)
                                   );

            _output = xml.ToString();
        }
    }
}
