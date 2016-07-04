using Datavail.Delta.Cloud.Mvc.Infrastructure.Startup;
using StructureMap.Configuration.DSL;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure
{
    public class TaskRegistry : Registry
    {
        public TaskRegistry()
        {
            Scan(x =>
            {
                x.TheCallingAssembly();
                x.AddAllTypesOf<IRunAtStartup>();
            });
        }
    }
}