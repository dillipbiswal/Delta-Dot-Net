using System.Web.Mvc;
using StructureMap;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.Startup
{
    public class AspNetBootstrapper : IRunAtStartup
    {
        private readonly IContainer _container;

        public AspNetBootstrapper(IContainer container)
        {
            _container = container;
        }

        public void Execute()
        {
            //TODO: Refactor this to use the IoC container and some sort of filter provider method. 
            GlobalFilters.Filters.Add(new HandleErrorAttribute());

            AreaRegistration.RegisterAllAreas();
            DependencyResolver.SetResolver(new StructureMapDependencyResolver(_container));
            ValueProviderFactories.Factories.Add(new JsonValueProviderFactory());
        }
    }
}