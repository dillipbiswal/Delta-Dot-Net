using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
	public class SecurityRegistry : Registry
	{
		public SecurityRegistry()
		{
            //For<CurrentUser>().Add<CurrentUser>();

            //For(typeof(IRepository<Project>))
            //    .EnrichWith((ctx, obj) => new ProjectSecurityDecorator((IRepository<Project>)obj, ctx.GetInstance<CurrentUser>()));

            //For(typeof(IRepository<Issue>))
            //    .EnrichWith((ctx, obj) => new IssueSecurityDecorator((IRepository<Issue>)obj, ctx.GetInstance<CurrentUser>()));
		}
	}
}