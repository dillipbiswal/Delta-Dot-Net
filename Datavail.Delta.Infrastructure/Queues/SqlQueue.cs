using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web.Script.Serialization;

namespace Datavail.Delta.Infrastructure.Queues
{
    public class SqlQueue<TMessage> : IQueue<TMessage> where TMessage : QueueMessage
    {
        const string ADD_MESSAGE_SQL = "INSERT INTO {0} (Data) VALUES (@binaryValue)";
        const string CLEAR_QUEUE_SQL = "DELETE FROM {0}";
        const string COUNT_SQL = "SELECT COUNT(*) FROM {0}";
        const string DELETE_MESSAGE_SQL = "DELETE FROM {0} WHERE [MessageId]={1}";

        private readonly string _connectionString;
        private readonly string _tableName;

        public SqlQueue(string queueTableName)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["QueuesConnectionString"].ConnectionString;
            _tableName = queueTableName;
        }

        public void AddMessage(TMessage message)
        {
            var serializedMessage = new JavaScriptSerializer().Serialize(message);
            var commandText = string.Format(ADD_MESSAGE_SQL, _tableName);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                connection.Open();
                command.Parameters.Add("@binaryValue", SqlDbType.VarBinary).Value = Encoding.ASCII.GetBytes(serializedMessage);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public void Clear()
        {
            var commandText = string.Format(CLEAR_QUEUE_SQL, _tableName);
            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void DeleteMessage(TMessage message)
        {
            var commandText = string.Format(DELETE_MESSAGE_SQL, _tableName, message.PopReceipt);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public int GetApproximateMessageCount()
        {
            var count = 0;
            var commandText = string.Format(COUNT_SQL, _tableName);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                connection.Open();
                count = (int)command.ExecuteScalar();
                connection.Close();
            }

            return count;
        }

        public TMessage GetMessage()
        {
            var commandText = "Get" + _tableName + "Row";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();
                
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        var id = (int)reader["MessageId"];
                        var count = (int)reader["DequeueCount"];
                        var data = (byte[])reader["Data"];

                        var deserializedMessage = new JavaScriptSerializer().Deserialize<TMessage>(Encoding.ASCII.GetString(data));
                        deserializedMessage.PopReceipt = (ulong)id;
                        deserializedMessage.DequeueCount = count;
                        connection.Close();

                        return deserializedMessage;
                    }
                }

                connection.Close();
            }
            return null;
        }

        public void Dispose()
        {

        }
    }
}
