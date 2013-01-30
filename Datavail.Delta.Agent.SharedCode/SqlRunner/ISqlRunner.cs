using System.Data;

namespace Datavail.Delta.Agent.SharedCode.SqlRunner
{
    public interface ISqlRunner
    {
        IDataReader RunSql(string connectionString, string sql);
    }
}
