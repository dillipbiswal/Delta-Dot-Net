using System.ServiceModel;

namespace Datavail.Delta.Cloud.Ws
{
    public class WcfServiceInstanceExtension : IExtension<InstanceContext>
    {
        public InstanceItems Items = new InstanceItems();

        public static WcfServiceInstanceExtension Current
        {
            get
            {
                if (OperationContext.Current == null)
                    return null;

                var instanceContext = OperationContext.Current.InstanceContext;
                return GetExtensionFrom(instanceContext);
            }
        }

        public static WcfServiceInstanceExtension GetExtensionFrom(InstanceContext instanceContext)
        {
            lock (instanceContext)
            {
                var extension = instanceContext.Extensions.Find<WcfServiceInstanceExtension>();
                if (extension == null)
                {
                    extension = new WcfServiceInstanceExtension();
                    extension.Items.Hook(instanceContext);

                    instanceContext.Extensions.Add(extension);
                }
                return extension;
            }
        }
        
        public void Attach(InstanceContext owner)
        { }

        public void Detach(InstanceContext owner)
        { }
    }
}