using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;

namespace Datavail.Delta.Agent.Plugin.SqlServer2005
{
    internal class SqlHelper
    {
        public static IDataReader GetDataReader(SqlConnection connection, string sql)
        {
            var command = new SqlCommand(sql, connection);
            command.CommandType = CommandType.Text;

            connection.Open();
            var result = command.ExecuteReader();

            return result;
        }
    }
}