using Datavail.Delta.Repository.EfWithMigrations;
using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
	public class DefaultConventionsRegistry : Registry
	{
		public DefaultConventionsRegistry()
		{
			Scan(scan =>
					{
						scan.TheCallingAssembly();
						scan.AssembliesFromApplicationBaseDirectory(assembly => assembly.FullName.Contains("Datavail"));
						scan.WithDefaultConventions();
					});
		}
	}
}