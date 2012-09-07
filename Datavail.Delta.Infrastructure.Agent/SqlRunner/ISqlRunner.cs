using System.Data;

namespace Datavail.Delta.Infrastructure.Agent.SqlRunner
{
    public interface ISqlRunner
    {
        IDataReader RunSql(string connectionString, string sql);
    }
}
