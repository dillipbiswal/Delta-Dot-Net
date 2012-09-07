using System.Data;
using System.Data.SqlClient;

namespace Datavail.Delta.Agent.SharedCode.SqlRunner
{
    public class SqlServerRunner : ISqlRunner
    {
        public IDataReader RunSql(string connectionString, string sql)
        {
            var conn = new SqlConnection(connectionString);
            var command = new SqlCommand(sql, conn);
            command.CommandType = CommandType.Text;

            conn.Open();
            var result = command.ExecuteReader();

            return result;
        }
    }
}
