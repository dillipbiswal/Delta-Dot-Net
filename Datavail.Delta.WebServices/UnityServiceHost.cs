using System;
using System.ServiceModel;

namespace Datavail.Delta.WebServices
{
    public class UnityServiceHost : ServiceHost
    {
        public UnityServiceHost()
            : base()
        {
        }

        public UnityServiceHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        protected override void OnOpening()
        {
            this.Description.Behaviors.Add(new UnityInstanceProviderServiceBehavior());
            base.OnOpening();
        }
    }
}
