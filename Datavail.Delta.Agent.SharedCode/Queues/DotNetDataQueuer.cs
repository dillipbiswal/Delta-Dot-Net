using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using System;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public class DotNetDataQueuer : IDataQueuer
    {
        private readonly IDeltaLogger _logger;

        public DotNetDataQueuer()
        {
            _logger = new DeltaLogger();
        }

        public void Queue(string data)
        {
            try
            {
                var msg = new QueueMessage() {Data = data, Timestamp = DateTime.UtcNow};
                DotNetDataQueuerFactory.Current.Add(msg);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception Occurred in DotNetDataQueuer.Queue", ex);
            }
        }
    }
}