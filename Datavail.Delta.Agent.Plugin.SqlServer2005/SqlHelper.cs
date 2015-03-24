using System.Data;
using System.Data.SqlClient;

namespace Datavail.Delta.Agent.Plugin.SqlServer2005
{
    internal class SqlHelper
    {
        public static IDataReader GetDataReader(SqlConnection connection, string sql)
        {
            var command = new SqlCommand(sql, connection) {CommandType = CommandType.Text};

            connection.Open();
            var result = command.ExecuteReader();

            return result;
        }
    }
}