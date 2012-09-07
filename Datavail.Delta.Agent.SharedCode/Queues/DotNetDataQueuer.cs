using System;
using System.Collections.Concurrent;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public class DotNetDataQueuer : IDataQueuer
    {
        private static BlockingCollection<QueueMessage> _cache;
        private readonly IDeltaLogger _logger;

        public DotNetDataQueuer()
        {
            _cache = DotNetDataQueuerFactory.Current;
            _logger = new DeltaLogger();
        }

        public void Queue(string data)
        {
            try
            {
                var msg = new QueueMessage() {Data = data, Timestamp = DateTime.UtcNow};
                _cache.Add(msg);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception Occurred in DotNetDataQueuer.Queue", ex);
            }
        }
    }
}