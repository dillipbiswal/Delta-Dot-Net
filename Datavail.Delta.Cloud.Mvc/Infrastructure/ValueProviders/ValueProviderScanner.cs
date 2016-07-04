using System;
using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Utility;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ValueProviders
{
    public class ValueProviderScanner : IRegistrationConvention
    {
        public void Process(Type type, Registry registry)
        {
            if (type.CanBeCastTo<IValueProvider>())
            {
                var factoryType = typeof(StructureMapValueProviderFactory<>).MakeGenericType(type);
                registry.For<ValueProviderFactory>().Use(c => (ValueProviderFactory)c.GetInstance(factoryType));
            }
        }
    }
}