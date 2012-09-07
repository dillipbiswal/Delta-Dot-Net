using System;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.SqlRunner;

namespace Datavail.Delta.Infrastructure.Agent.ServerInfo
{

    public class SqlServerInfo : IDatabaseServerInfo
    {
        private readonly ISqlRunner _sqlRunner;
        private readonly IDeltaLogger _logger;

        public string Product { get; set; }
        public string ProductVersion { get; set; }
        public string ProductLevel { get; set; }
        public string ProductEdition { get; set; }

        //Specific
        private readonly string _connectionString;


        public SqlServerInfo(string connectionString)
        {
            _connectionString = connectionString;
            _sqlRunner = new SqlServerRunner();
            _logger = new DeltaLogger();

            GetDatabaseServerInfo();
        }

        public SqlServerInfo(string connectionString, ISqlRunner sqlRunner, IDeltaLogger logger)
        {
            _connectionString = connectionString;
            _sqlRunner = sqlRunner;
            _logger = logger;

            GetDatabaseServerInfo();
        }

        private void GetDatabaseServerInfo()
        {
            _logger.LogDebug("SqlServerInfo requested");
            try
            {
                const string SQL = "SELECT SERVERPROPERTY('productversion') as ProductVersion, SERVERPROPERTY ('productlevel') as ProductLevel, SERVERPROPERTY ('edition') as Edition";
                var result = _sqlRunner.RunSql(_connectionString, SQL);

                if (result.FieldCount > 0 && result.Read())
                {
                    Product = "SQL Server";
                    ProductVersion = result[0].ToString();
                    ProductLevel = result[1].ToString();
                    ProductEdition = result[2].ToString();

                    _logger.LogDebug("SqlServerInfo returned");
                }
                else
                {
                    _logger.LogDebug("No SqlServerInfo returned");
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error retrieving Sql Server info.", ex);
            }
        }
    }
}
