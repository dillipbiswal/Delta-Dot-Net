
using Datavail.Delta.Infrastructure.Agent.Common;

namespace Datavail.Delta.Infrastructure.Agent.Queues
{
    public static class QueueFactory
    {
        private static readonly ICommon Common = new Common.Common();
        private static readonly IPersistentQueue Queue = new PersistentQueue(Common.GetCachePath());
        public static IPersistentQueue Current
        {
            get
            {
                return Queue;
            }
        }
    }
}
