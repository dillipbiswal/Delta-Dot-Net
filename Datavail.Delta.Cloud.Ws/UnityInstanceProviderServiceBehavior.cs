using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Datavail.Delta.Cloud.Ws
{
   public class UnityInstanceProviderServiceBehavior : IServiceBehavior
      {
          #region IServiceBehavior Members
  
          public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
          {
          }
  
          public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
          {
              serviceHostBase.ChannelDispatchers.ToList().ForEach(channelDispatcher =>
              {
                  var dispatcher = channelDispatcher as ChannelDispatcher;
  
                  if (dispatcher != null)
                  {
                      dispatcher.Endpoints.ToList().ForEach(endpoint =>
                      {
                          endpoint.DispatchRuntime.InstanceProvider = new UnityInstanceProvider(serviceDescription.ServiceType);
                      });
                  }
              });
          }
  
          public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
          {
          }
  
          #endregion
      }
  }