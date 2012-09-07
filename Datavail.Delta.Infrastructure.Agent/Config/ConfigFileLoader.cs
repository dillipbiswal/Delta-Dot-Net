
namespace Datavail.Delta.Agent.Scheduler
{
    public class ConfigFileLoader : IConfigLoader
    {
        public string LoadConfig(string path)
        {
            var file = System.IO.File.ReadAllText(path);
            return file;
        }
    }
}
