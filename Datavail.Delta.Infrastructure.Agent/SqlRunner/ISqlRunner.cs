using System.Data;
using System.Data.SqlClient;

namespace Datavail.Delta.Infrastructure.Agent.SqlRunner
{
    public interface ISqlRunner
    {
        IDataReader RunSql(string connectionString, string sql);
        IDataReader GetDataReader(SqlConnection connection, string sql);
    }
}
