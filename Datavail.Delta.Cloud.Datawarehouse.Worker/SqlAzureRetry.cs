using System;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;
using PostSharp.Aspects;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker
{
    [Serializable]
    public class SqlAzureRetry : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            var policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(10, TimeSpan.FromMilliseconds(100));
            policy.ExecuteAction(args.Proceed);
        }
    }
}