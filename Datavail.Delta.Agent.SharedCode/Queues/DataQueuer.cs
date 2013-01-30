using System;
using Datavail.Delta.Agent.SharedCode.Logging;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public class DataQueuer : IDataQueuer
    {
        private static IPersistentQueue _cache;
        private readonly IDeltaLogger _logger;

        public DataQueuer()
        {
            _cache = QueueFactory.Current;
            _logger = new DeltaLogger();
        }

        public void Queue(string data)
        {
            try
            {
                using (var session = _cache.OpenSession())
                {
                    var msg = new QueueMessage() { Data = data, Timestamp = DateTime.UtcNow };
                    session.Enqueue(msg.ToByteArray());
                    session.Flush();
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception Occurred in DataQueuer.Queue", ex);
            }
        }
    }
}
