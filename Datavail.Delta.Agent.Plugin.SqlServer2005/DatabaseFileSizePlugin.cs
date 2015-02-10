using System;
using System.Collections.Generic;
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
    public class DatabaseFileSizePlugin : IPlugin
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
        private string _databaseName;
        private string _instanceName;
        private string _clusterGroupName;
        private bool _runningOnCluster = false;


        public DatabaseFileSizePlugin()
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

        public DatabaseFileSizePlugin(IClusterInfo clusterInfo, IDataQueuer dataQueuer, IDeltaLogger logger, ISqlRunner sqlRunner, IDatabaseServerInfo databaseServerInfo)
        {
            _clusterInfo = clusterInfo;
            _dataQueuer = dataQueuer;
            _sqlRunner = sqlRunner;
            _databaseServerInfo = databaseServerInfo;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("DatabaseFileSize.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
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

                    GetDatabaseFileSize();

                    if (_output != null)
                    {
                        _dataQueuer.Queue(_output);
                        _logger.LogDebug("Data Queued: " + _output);
                    }
                    else
                    {
                        _logger.LogDebug("No Data Queued: No database file size data colleced for database " + _databaseName);
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
            _databaseName = xmlData.Attribute("DatabaseName").Value;
            _instanceName = xmlData.Attribute("InstanceName").Value;

            if (xmlData.Attribute("ClusterGroupName") != null)
            {
                _runningOnCluster = true;
                _clusterGroupName = xmlData.Attribute("ClusterGroupName").Value;
            }


            if (_databaseServerInfo == null)
                _databaseServerInfo = new SqlServerInfo(_connectionString);
        }

        private void GetDatabaseFileSize()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("USE [" + _databaseName + "]; ");
            sql.Append("SELECT f.name as FileName, case when f.groupid = 0 then 'Log' else 'Rows Data' end as FileGroupType, ");
            sql.Append("case when g.groupname is null then 'Not Applicable' else g.groupname end as FileGroup, ");
            sql.Append("f.size*8 as 'InitialSize', fileproperty(f.name,'SpaceUsed')*8 as 'SpaceUsed', f.growth as 'Growth', f.status as 'Status', f.maxsize as 'MaxSize',");
            sql.Append("upper(substring(f.filename,1,1)) as Disk, f.filename as FilePath ");
            sql.Append("FROM dbo.sysfiles f  (nolock) ");
            sql.Append("LEFT JOIN dbo.sysfilegroups g  (nolock) on (f.groupid=g.groupid)");

            var result = _sqlRunner.RunSql(_connectionString, sql.ToString());

            var gotData = false;

            var fileNames = new List<String>();
            var fileGroupTypes = new List<String>();
            var fileGroups = new List<String>();
            var initialSizes = new List<String>();
            var spaceUsed = new List<String>();
            var growth = new List<String>();
            var statuses = new List<String>();
            var maxSizes = new List<String>();
            var disks = new List<String>();
            var filePaths = new List<String>();

            while (result.Read())
            {
                gotData = true;
                fileNames.Add(result["FileName"].ToString());
                fileGroupTypes.Add(result["FileGroupType"].ToString());
                fileGroups.Add(result["FileGroup"].ToString());
                initialSizes.Add(result["InitialSize"].ToString());
                spaceUsed.Add(result["SpaceUsed"].ToString());
                growth.Add(result["Growth"].ToString());
                statuses.Add(result["Status"].ToString());
                maxSizes.Add(result["MaxSize"].ToString());
                disks.Add(result["Disk"].ToString());
                filePaths.Add(result["FilePath"].ToString());

                resultCode = "0";
                resultMessage = "File size returned for database: " + _databaseName;
            }

            if (!gotData)
            {
                resultMessage = "File size not found for: " + _databaseName;
                fileNames.Add("n/a");
                fileGroupTypes.Add("n/a");
                fileGroups.Add("n/a");
                initialSizes.Add("-1");
                spaceUsed.Add("-1");
                growth.Add("0");
                statuses.Add("0");
                maxSizes.Add("0");
                disks.Add("n/a");
                filePaths.Add("n/a");
            }
            
            BuildExecuteOutput(_databaseName, fileNames, fileGroupTypes, fileGroups, initialSizes, spaceUsed, growth, statuses, maxSizes, disks, filePaths, resultCode, resultMessage);
        }

        private void BuildExecuteOutput(string databaseName, List<String> fileNames, List<String> fileGroupTypes, List<String> fileGroups, List<String> initialSizes, List<String> spaceUsed, List<String> growth,
            List<String> statuses, List<String> maxSizes, List<String> disks, List<String> filePaths, string resultCode, string resultMessage)
        {

            var xml = new XElement("DatabaseFileSizePluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("databaseName", databaseName),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition));
            var files = new XElement("Files", "");

            for (var i = 0; i < fileNames.Count; i++)
                files.Add(new XElement("File",
                                   new XAttribute("fileName", fileNames[i]),
                                   new XAttribute("fileGroupType", fileGroupTypes[i]),
                                   new XAttribute("fileGroup", fileGroups[i]),
                                   new XAttribute("initialSize", initialSizes[i]),
                                   new XAttribute("spaceUsed", spaceUsed[i]),
                                   new XAttribute("growth", growth[i]),
                                   new XAttribute("status", statuses[i]),
                                   new XAttribute("maxSize", maxSizes[i]),
                                   new XAttribute("disk", disks[i]),
                                   new XAttribute("filePath", filePaths[i])));

            xml.Add(files);

            _output = xml.ToString();
        }


    }
}
