using System;
using System.Threading;
using PostSharp.Aspects;

namespace Datavail.Delta.AdministratorUtility
{
    [Serializable]
    public sealed class OnWorkerThreadAttribute : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs eventArgs)
        {
            ThreadPool.QueueUserWorkItem(delegate { eventArgs.Proceed(); });
        }
    }
}