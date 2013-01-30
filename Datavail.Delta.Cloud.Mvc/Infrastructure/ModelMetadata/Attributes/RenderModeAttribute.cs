using System;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.ModelMetadata.Attributes
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class RenderModeAttribute : Attribute
	{
		public RenderMode RenderMode { get; set; }

		public RenderModeAttribute(RenderMode renderMode)
		{
			RenderMode = renderMode;
		}
	}
}