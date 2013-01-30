using System;
using System.Collections.Generic;
using System.Linq;
using Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata.Attributes;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata.Filters
{
	public class RenderModeFilter : IModelMetadataFilter
	{
		public void TransformMetadata(System.Web.Mvc.ModelMetadata metadata, IEnumerable<Attribute> attributes)
		{
			if (attributes.OfType<RenderModeAttribute>().Any())
			{
				switch (attributes.OfType<RenderModeAttribute>().First().RenderMode)
				{
					case RenderMode.None:
						metadata.ShowForDisplay = false;
						metadata.ShowForEdit = false;
						break;
				}
			}
		}
	}
}