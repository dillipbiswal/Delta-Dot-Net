﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata.Filters;
using System.Linq;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata
{
	public class SolidModelMetadataProvider : DataAnnotationsModelMetadataProvider
	{
		private readonly IModelMetadataFilter[] _metadataFilters;

		public SolidModelMetadataProvider(IModelMetadataFilter[] metadataFilters)
		{
			_metadataFilters = metadataFilters;
		}

		protected override System.Web.Mvc.ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
		{
			var metadata = base.CreateMetadata(attributes, containerType, modelAccessor, modelType, propertyName);

			//_metadataFilters.ForEach(m => m.TransformMetadata(metadata, attributes));

			return metadata;
		}
	}
}