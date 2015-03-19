using System;
using System.Data.SqlClient;
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
        }

        private void GetDatabaseFileSize()
        {
            var resultCode = "-1";
            var resultMessage = string.Empty;

            var sql = new StringBuilder();
            sql.Append("USE [" + _databaseName + "]; ");
            sql.Append("SELECT TOP 100 percent ");
            sql.Append("[DATABASE_NAME] = db_name(), ");
            sql.Append("[FILEGROUP_TYPE] = case when a.groupid = 0 then 'Log' else 'Data' end, ");
            sql.Append("[FILEGROUP_ID] = a.groupid, a.[FILEGROUP], [FILEID] = a.fileid, ");
            sql.Append("[FILENAME] = a.name, [DISK] = upper(substring(a.filename,1,1)), ");
            sql.Append("[FILEPATH] = a.filename, ");
            sql.Append("[MAX_FILE_SIZE] = convert(int,round( (case a.maxsize when -1 then null else a.maxsize end*1.000)/128.000 ,0)), ");
            sql.Append("[FILE_SIZE] = a.[fl_size], [FILE_SIZE_USED] = a.[fl_used], [FILE_SIZE_UNUSED] = a.[fl_unused], ");
            sql.Append("[DATA_SIZE] = case when a.groupid <> 0 then a.[fl_size] else 0 end, ");
            sql.Append("[DATA_SIZE_USED] = case when a.groupid <> 0 then a.[fl_used] else 0 end, ");
            sql.Append("[DATA_SIZE_UNUSED]     = case when a.groupid <> 0 then a.[fl_unused] else 0 end, ");
            sql.Append("[LOG_SIZE] = case when a.groupid = 0 then a.[fl_size] else 0 end, ");
            sql.Append("[LOG_SIZE_USED] = case when a.groupid = 0 then a.[fl_used] else 0 end, ");
            sql.Append("[LOG_SIZE_UNUSED] = case when a.groupid = 0 then a.[fl_unused] else 0 end ");
            sql.Append("FROM ");
            sql.Append("( select  aa.*, [FILEGROUP]    = isnull(bb.groupname, ''),  ");
            sql.Append("[fl_size] = convert(int,round((aa.size*1.000)/128.000,0)),  ");
            sql.Append("[fl_used]  = convert(int,round(fileproperty(aa.name,'SpaceUsed')/128.000,0)),   ");
            sql.Append("[fl_unused]  = convert(int,round((aa.size-fileproperty(aa.name,'SpaceUsed'))/128.000,0))  ");
            sql.Append("from dbo.sysfiles aa (nolock) ");
            sql.Append("left join dbo.sysfilegroups bb (nolock) on ( aa.groupid = bb.groupid )) a ");

            using (var conn = new SqlConnection(_connectionString))
            {
                var result = SqlHelper.GetDataReader(conn, sql.ToString());

                if (result.FieldCount > 0)
                {
                    while (result.Read())
                    {
                        var databaseName = result["DATABASE_NAME"].ToString();
                        var fileGroupType = result["FILEGROUP_TYPE"].ToString();
                        var fileGroupId = result["FILEGROUP_ID"].ToString();
                        var fileGroup = result["FILEGROUP"].ToString();
                        var fileId = result["FILEID"].ToString();
                        var fileName = result["FILENAME"].ToString();
                        var disk = result["DISK"].ToString();
                        var filePath = result["FILEPATH"].ToString();
                        var maxFileSize = result["MAX_FILE_SIZE"].ToString();
                        var fileSize = result["FILE_SIZE"].ToString();
                        var fileSizeUsed = result["FILE_SIZE_USED"].ToString();
                        var fileSizeUnused = result["FILE_SIZE_UNUSED"].ToString();
                        var dataSize = result["DATA_SIZE"].ToString();
                        var dataSizeUsed = result["DATA_SIZE_USED"].ToString();
                        var dataSizeUnused = result["DATA_SIZE_UNUSED"].ToString();
                        var logSize = result["LOG_SIZE"].ToString();
                        var logSizeUsed = result["LOG_SIZE_USED"].ToString();
                        var logSizeUnused = result["LOG_SIZE_UNUSED"].ToString();

                        resultCode = "0";
                        resultMessage = "File size returned for database: " + _databaseName;

                        BuildExecuteOutput(databaseName, fileGroupType, fileGroupId, fileGroup, fileId, fileName, disk, filePath,
                            maxFileSize, fileSize, fileSizeUsed, fileSizeUnused, dataSize, dataSizeUsed, dataSizeUnused,
                            logSize, logSizeUsed, logSizeUnused, resultCode, resultMessage);
                    }
                }
                else
                {
                    resultMessage = "File size not found for: " + _databaseName;

                    BuildExecuteOutput("0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", resultCode, resultMessage);
                }
            }
        }

        private void BuildExecuteOutput(string databaseName, string fileGroupType, string fileGroupId, string fileGroup,
            string fileId, string fileName, string disk, string filePath, string maxFileSize, string fileSize,
            string fileSizeUsed, string fileSizeUnused, string dataSize, string dataSizeUsed, string dataSizeUnused,
            string logSize, string logSizeUsed, string logSizeUnused, string resultCode, string resultMessage)
        {

            var xml = new XElement("DatabaseFileSizePluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", resultCode),
                                   new XAttribute("resultMessage", resultMessage),
                                   new XAttribute("name", databaseName),
                                   new XAttribute("instanceName", _instanceName),
                                   new XAttribute("product", _databaseServerInfo.Product),
                                   new XAttribute("productVersion", _databaseServerInfo.ProductVersion),
                                   new XAttribute("productLevel", _databaseServerInfo.ProductLevel),
                                   new XAttribute("productEdition", _databaseServerInfo.ProductEdition),
                                   new XAttribute("fileGroupType", fileGroupType),
                                   new XAttribute("fileGroupId", fileGroupId),
                                   new XAttribute("fileGroup", fileGroup),
                                   new XAttribute("fileId", fileId),
                                   new XAttribute("fileName", fileName),
                                   new XAttribute("disk", disk),
                                   new XAttribute("filePath", filePath),
                                   new XAttribute("maxFileSize", maxFileSize),
                                   new XAttribute("fileSize", fileSize),
                                   new XAttribute("fileSizeUsed", fileSizeUsed),
                                   new XAttribute("fileSizeUnused", fileSizeUnused),
                                   new XAttribute("dataSize", dataSize),
                                   new XAttribute("dataSizeUsed", dataSizeUsed),
                                   new XAttribute("dataSizeUnused", dataSizeUnused),
                                   new XAttribute("logSize", logSize),
                                   new XAttribute("logSizeUsed", logSizeUsed),
                                   new XAttribute("logSizeUnused", logSizeUnused));


            _output = xml.ToString();
        }


    }
}
