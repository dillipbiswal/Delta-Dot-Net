using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata;
using Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata.Filters;
using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
	public class ModelMetadataRegistry : Registry
	{
		public ModelMetadataRegistry()
		{
			For<ModelMetadataProvider>().Use<SolidModelMetadataProvider>();

			Scan(scan =>
			     	{
			     		scan.TheCallingAssembly();
			     		scan.AddAllTypesOf<IModelMetadataFilter>();
			     	});
		}
	}
}