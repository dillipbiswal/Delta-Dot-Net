using System.Data.Entity;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Datavail.Delta.Repository.Interface;
using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
	public class EfRegistry : Registry
	{
		public EfRegistry()
		{
            For(typeof(DbContext)).HttpContextScoped().Use(typeof(DeltaDbContext));
            For(typeof(IRepository)).Use(typeof(GenericRepository));
            For(typeof(IServerRepository)).Use(typeof(ServerRepository));		
		}
	}
}