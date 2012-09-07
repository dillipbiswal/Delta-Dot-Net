using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Datavail.Delta.Cloud.Mvc.Utility;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata.Filters
{
	public class PascalCaseToDisplayNameFilter : IModelMetadataFilter
	{
		public void TransformMetadata(System.Web.Mvc.ModelMetadata metadata, IEnumerable<Attribute> attributes)
		{
			if (!string.IsNullOrEmpty(metadata.PropertyName) && !attributes.OfType<DisplayNameAttribute>().Any())
			{
				metadata.DisplayName = metadata.PropertyName.ToStringWithSpaces();
			}
		}
	}
}