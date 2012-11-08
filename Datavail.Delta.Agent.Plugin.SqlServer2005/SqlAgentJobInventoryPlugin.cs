using System;
using System.Data;
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

    public class SqlAgentJobInventoryPlugin : IPlugin
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
        private string _instanceId;
        private bool _runningOnCluster;


        public SqlAgentJobInventoryPlugin()
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

        public SqlAgentJobInventoryPlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        private void ParseData(string data)
        {
            var crypto = new Encryption();
            var xmlData = XElement.Parse(data);

            Guard.ArgumentNotNull(xmlData.Attribute("ConnectionString"), "ConnectionString");
            Guard.ArgumentNotNull(xmlData.Attribute("InstanceName"), "InstanceName");
            Guard.ArgumentNotNull(xmlData.Attribute("InstanceId"), "InstanceId");

            // ReSharper disable PossibleNullReferenceException
            _connectionString = crypto.DecryptString(xmlData.Attribute("ConnectionString").Value);

            _instanceName = xmlData.Attribute("InstanceName").Value;
            _instanceId = xmlData.Attribute("InstanceId").Value;
            // ReSharper restore PossibleNullReferenceException

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                // ReSharper disable PossibleNullReferenceException
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
                // ReSharper restore PossibleNullReferenceException
            }
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("SqlAgentJobInventory.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}", metricInstance, label, data));
            try
            {
                const string resultCode = "0";
                var resultMessage = string.Empty;

                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                if (!_runningOnCluster || (_runningOnCluster && _clusterInfo.IsActiveClusterNodeForGroup(_clusterGroupName)))
                {
                    if (_databaseServerInfo == null)
                        _databaseServerInfo = new SqlServerInfo(_connectionString);

                    const string sql = "SELECT name FROM sysjobs";
                    var result = _sqlRunner.RunSql(_connectionString, sql);

                    BuildExecuteOutput(result, resultCode, resultMessage);

                    _dataQueuer.Queue(_output);
                    _logger.LogDebug("Data Queued: " + _output);
                }

            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }
        }

        private void BuildExecuteOutput(IDataReader dataReader, string resultCode, string resultMessage)
        {
            var xml = new XDocument(new XElement("SqlAgentJobInventoryPluginOutput",
                 new XAttribute("product", _databaseServerInfo.Product),
                 new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                 new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                 new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                 new XAttribute("timestamp", DateTime.UtcNow),
                 new XAttribute("metricInstanceId", _metricInstance),
                 new XAttribute("label", _label),
                 new XAttribute("instanceId", _instanceId),
                 new XAttribute("instanceName", _instanceName),
                 new XAttribute("resultCode", resultCode),
                 new XAttribute("resultMessage", resultMessage)));

            if (dataReader.FieldCount > 0)
            {
                while (dataReader.Read())
                {
                    var name = dataReader["name"].ToString();
                    var element = new XElement("SqlAgentJob", new XAttribute("name", name));
                    if (xml.Root != null) xml.Root.Add(element);
                }
            }

            _output = xml.ToString();
        }
    }
}