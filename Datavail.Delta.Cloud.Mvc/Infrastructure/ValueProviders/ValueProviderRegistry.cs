using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ValueProviders
{
    public class ValueProviderRegistry : Registry
    {
        public ValueProviderRegistry()
        {
            Scan(scanner =>
            {
                scanner.TheCallingAssembly();
                scanner.Convention<ValueProviderScanner>();
            });
        }
    }
}