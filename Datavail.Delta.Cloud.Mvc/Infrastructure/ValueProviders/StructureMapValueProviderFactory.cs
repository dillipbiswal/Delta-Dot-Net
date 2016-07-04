using System.Web.Mvc;
using StructureMap;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ValueProviders
{
    public class StructureMapValueProviderFactory<T> : ValueProviderFactory where T : IValueProvider
    {
        private readonly IContainer _container;

        public StructureMapValueProviderFactory(IContainer container)
        {
            _container = container;
        }

        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            return _container.GetInstance<T>();
        }
    }
}