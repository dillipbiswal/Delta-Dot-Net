using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace Datavail.Delta.WebServices
{
    public class UnityServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new UnityServiceHost(serviceType, baseAddresses);
        }
    }
}
