using System;
using Datavail.Delta.Agent.SharedCode.Logging;
using Datavail.Delta.Agent.SharedCode.SqlRunner;

namespace Datavail.Delta.Agent.SharedCode.ServerInfo
{
    
    public class OracleServerInfo : IDatabaseServerInfo
    {
        private readonly ISqlRunner _sqlRunner;
        private readonly IDeltaLogger _logger;

        public string Product { get; set; }
        public string ProductVersion { get; set; }
        public string ProductLevel { get; set; }
        public string ProductEdition { get; set; }
        
        //Specific
        private string _connectionString;
        

        public OracleServerInfo(string connectionString)
        {
            _connectionString = connectionString;
            _sqlRunner = new SqlServerRunner();
            _logger = new DeltaLogger();

            GetDatabaseServerInfo();
        }

        public OracleServerInfo(string connectionString, ISqlRunner sqlRunner, IDeltaLogger logger)
        {
            _connectionString = connectionString;
            _sqlRunner = sqlRunner;
            _logger = logger;

            GetDatabaseServerInfo();
        }

        private void GetDatabaseServerInfo()
        {
            _logger.LogDebug(String.Format("OracleServerInfo requested using connectionstring: {0} ",
                                            _connectionString));
            
        }
    }
}
