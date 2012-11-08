using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Datavail.Delta.Infrastructure.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Datavail.Delta.Infrastructure.Queues
{
    public class RabbitMqQueue<TMessage> : IQueue<TMessage> where TMessage : QueueMessage
    {
        private readonly IDeltaLogger _logger;
        private readonly int _maxRetries;
        private readonly string _queueName;
        private readonly string _hostName;
        private int _retryCounter;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _sharedConnection;
        private IModel _sharedModel;

        public RabbitMqQueue(IDeltaLogger logger, string hostName, string queueName, int maxRetries = 5)
        {
            _logger = logger;
            _queueName = queueName;
            _hostName = hostName;
            _maxRetries = maxRetries;

            _connectionFactory = new ConnectionFactory { HostName = _hostName };

            _sharedConnection = GetConnection();
            _sharedModel = GetModel(_sharedConnection);
        }

        private IConnection GetConnection()
        {
            try
            {
                var conn = _connectionFactory.CreateConnection(5);
                _retryCounter = 0;
                return conn;
            }
            catch (BrokerUnreachableException ex)
            {
                if (_retryCounter < _maxRetries)
                {
                    _logger.LogSpecificError(WellKnownErrorMessages.QueueEnpointConnectionFailed, string.Format("Could not connect to queue service. Retry #{0}", _retryCounter + 1), ex);
                    Thread.Sleep(TimeSpan.FromSeconds(3));

                    _retryCounter++;
                    return GetConnection();
                }

                _logger.LogSpecificError(WellKnownErrorMessages.QueueEnpointConnectionFailedAfterRetries, string.Format("Could not connect to queue service after {0} retries. Giving up.", _maxRetries));
                throw;
            }
        }

        private IModel GetModel(IConnection conn)
        {
            var model = conn.CreateModel();
            var result = DeclareQueue(model);

            return model;
        }

        private QueueDeclareOk DeclareQueue(IModel model)
        {
            var args = new Hashtable { { "x-ha-policy", "all" } };
            var result = model.QueueDeclare(_queueName, true, false, false, args);

            return result;
        }


        public void AddMessage(TMessage message)
        {
            using (var conn = GetConnection())
            {
                using (var model = GetModel(conn))
                {
                    var basicProperties = model.CreateBasicProperties();
                    basicProperties.SetPersistent(true);

                    var serializedMessage = new JavaScriptSerializer().Serialize(message);
                    model.BasicPublish("", _queueName, basicProperties, Encoding.ASCII.GetBytes(serializedMessage));
                }
            }
        }

        public void Clear()
        {
            using (var conn = GetConnection())
            {
                using (var model = GetModel(conn))
                {
                    model.QueuePurge(_queueName);
                }
            }
        }

        public void Delete()
        {
            using (var conn = GetConnection())
            {
                using (var model = GetModel(conn))
                {
                    model.QueueDelete(_queueName);
                }
            }
        }

        public void DeleteMessage(TMessage message) { _sharedModel.BasicAck(message.PopReceipt, false); }

        public int GetApproximateMessageCount()
        {
            using (var conn = GetConnection())
            {
                using (var model = GetModel(conn))
                {
                    var args = new Hashtable { { "x-ha-policy", "all" } };
                    var queue = model.QueueDeclare(_queueName, true, false, false, args);
                    return (int)queue.MessageCount;
                }
            }
        }

        public TMessage GetMessage()
        {
            var result = _sharedModel.BasicGet(_queueName, false);
            if (result == null)
            {
                return null;
            }

            var deserializedMessage = new JavaScriptSerializer().Deserialize<TMessage>(Encoding.ASCII.GetString(result.Body));
            deserializedMessage.PopReceipt = result.DeliveryTag;

            return deserializedMessage;
        }

        public void Dispose()
        {
            if (_sharedModel != null)
            {
                _sharedModel.Abort();
                _sharedModel.Dispose();
            }

            if (_sharedConnection != null)
            {
                _sharedConnection.Close();
                _sharedConnection.Dispose();
            }
        }
    }
}