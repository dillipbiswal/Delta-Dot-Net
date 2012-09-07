using System;
using System.ServiceModel;

namespace Datavail.Delta.Cloud.Ws
{
    public class UnityServiceHost : ServiceHost
    {
        public UnityServiceHost()
        {
        }

        public UnityServiceHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        protected override void OnOpening()
        {
            Description.Behaviors.Add(new UnityInstanceProviderServiceBehavior());
            base.OnOpening();
        }
    }
}
