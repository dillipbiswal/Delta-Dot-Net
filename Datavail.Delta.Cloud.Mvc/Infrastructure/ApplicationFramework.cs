using Datavail.Delta.Cloud.Mvc.Infrastructure.Startup;
using Datavail.Delta.Cloud.Mvc.Infrastructure.ValueProviders;
using StructureMap;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
    public class ApplicationFramework
    {
        public static void Bootstrap()
        {
            ObjectFactory.Initialize(x =>
            {
                x.AddRegistry<DefaultConventionsRegistry>();
                x.AddRegistry<ValueProviderRegistry>();
                x.AddRegistry<TaskRegistry>();
                x.AddRegistry<MvcRegistry>();
                x.AddRegistry<ModelMetadataRegistry>();
                x.AddRegistry<EfRegistry>();
                x.AddRegistry<SecurityRegistry>();
            });
        }

        public static void Start()
        {
            foreach (var task in ObjectFactory.GetAllInstances<IRunAtStartup>())
            {
                task.Execute();
            }
        }
    }
}