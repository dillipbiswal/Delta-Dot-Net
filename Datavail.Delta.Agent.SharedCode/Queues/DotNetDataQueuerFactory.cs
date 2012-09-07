using System.Collections.Concurrent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public static class DotNetDataQueuerFactory
    {
        private static readonly ICommon Common = new Common();
        private static readonly BlockingCollection<QueueMessage> Queue = new BlockingCollection<QueueMessage>();
        public static BlockingCollection<QueueMessage> Current
        {
            get
            {
                return Queue;
            }
        }
    }
}
