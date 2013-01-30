using Datavail.Delta.Agent.SharedCode.Common;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public static class QueueFactory
    {
        private static readonly ICommon Common = new Infrastructure.Agent.Common.Common();
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
