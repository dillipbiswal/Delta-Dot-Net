using Datavail.Delta.Application;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Repository.EfWithMigrations;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Datavail.Delta.ConfigBuilder
{

    public class ConfigGenerator
    {
        public static int RunningTasks;

        private readonly ServerService _serverService;
        private readonly string _configFilePath;
        private readonly ManualResetEvent _doneEvent;
        private readonly Guid _serverId;
        private readonly DeltaLogger _logger;

        public ConfigGenerator(Guid serverId, DeltaLogger logger, string configFilePath, ManualResetEvent doneEvent)
        {
            var context = new DeltaDbContext();
            var repository = new ServerRepository(context, logger);
            _serverService = new ServerService(logger, repository);

            _serverId = serverId;
            _logger = logger;
            _configFilePath = configFilePath;
            _doneEvent = doneEvent;
        }

        public void ConfigThreadPoolCallback(Object threadContext)
        {
            Console.WriteLine(DateTime.UtcNow + "(" + RunningTasks + ") Queueing configuration generation for ServerId " + _serverId);

            try
            {
                var config = _serverService.GetConfig(_serverId);
                var model = new ConfigModel
                {
                    Configuration = config,
                    GeneratingServer = Environment.MachineName,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                var json = JsonConvert.SerializeObject(model);
                var file = new StreamWriter(_configFilePath + "\\" + _serverId);
                file.WriteLine(json);
                file.Close();

                if (Interlocked.Decrement(ref RunningTasks) == 0)
                {
                    _doneEvent.Set();
                }

                Console.WriteLine(DateTime.UtcNow + "(" + RunningTasks + ") Completed configuration generation for ServerId " + _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("ConfigBuilder", ex);
                if (Interlocked.Decrement(ref RunningTasks) == 0)
                {
                    _doneEvent.Set();
                }
            }
        }

        public void AssemblyThreadPoolCallback(Object threadContext)
        {
            Console.WriteLine(DateTime.UtcNow + "(" + RunningTasks + ") Queueing assembly list generation for ServerId " + _serverId);

            try
            {
                var model = new AssemblyListModel
                {
                    GeneratingServer = Environment.MachineName,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                var assemblies = _serverService.GetAssembliesForServer(_serverId);
                foreach (var assembly in assemblies)
                {
                    var requiredAssembly = new AssemblyModel { AssemblyName = assembly.Key, Version = assembly.Value };
                    model.Assemblies.Add(requiredAssembly);
                }

                var json = JsonConvert.SerializeObject(model);
                var file = new StreamWriter(_configFilePath + "\\" + _serverId);
                file.WriteLine(json);
                file.Close();

                if (Interlocked.Decrement(ref RunningTasks) == 0)
                {
                    _doneEvent.Set();
                }

                Console.WriteLine(DateTime.UtcNow + "(" + RunningTasks + ") Completed assembly list generation for ServerId " + _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("ConfigBuilder", ex);
                if (Interlocked.Decrement(ref RunningTasks) == 0)
                {
                    _doneEvent.Set();
                }
            }
        }
    }

    internal class Program
    {
        private static string _configFilePath;
        private static string _assemblyFilePath;

        private static void BuildConfig()
        {
            _configFilePath = Directory.GetCurrentDirectory() + "\\config";
            CreateFolder(_configFilePath);
            
            var logger = new DeltaLogger();
            var context = new DeltaDbContext();

            var i = 0;
            var serverList = context.Servers.Where(s => s.Status != Status.Deleted && s.IsVirtual == false).ToList();

            //For testing -- just a single server
            //var serverId = Guid.Parse("413db431-106c-40de-ae4f-bf6599a4a47c");
            //var serverList = context.Servers.Where(s => s.Id == serverId).ToList();

            var configArray = new ConfigGenerator[serverList.Count];
            
            var config = new ManualResetEvent(false);
            foreach (var server in serverList)
            {
                try
                {
                    var c = new ConfigGenerator(server.Id, logger, _configFilePath, config);
                    configArray[i] = c;

                    Interlocked.Increment(ref ConfigGenerator.RunningTasks);
                    ThreadPool.QueueUserWorkItem(c.ConfigThreadPoolCallback, i);
                    i++;
                }
                catch (Exception ex)
                {
                    logger.LogUnhandledException("ConfigBuilder", ex);
                }
            }

            config.WaitOne();
        }

        private static void BuildAssembly()
        {
            _assemblyFilePath = Directory.GetCurrentDirectory() + "\\assemblylist";
            CreateFolder(_assemblyFilePath);

            var logger = new DeltaLogger();
            var context = new DeltaDbContext();

            var i = 0;
            var serverList = context.Servers.Where(s => s.Status != Status.Deleted && s.IsVirtual == false).ToList();

            //For testing -- just a single server
            //var serverId = Guid.Parse("413db431-106c-40de-ae4f-bf6599a4a47c");
            //var serverList = context.Servers.Where(s => s.Id == serverId).ToList();

            var assemblyArray = new ConfigGenerator[serverList.Count];

            var signal = new ManualResetEvent(false);
            foreach (var server in serverList)
            {
                try
                {
                    var c = new ConfigGenerator(server.Id, logger, _assemblyFilePath, signal);
                    assemblyArray[i] = c;

                    Interlocked.Increment(ref ConfigGenerator.RunningTasks);
                    ThreadPool.QueueUserWorkItem(c.AssemblyThreadPoolCallback, i);
                    i++;
                }
                catch (Exception ex)
                {
                    logger.LogUnhandledException("AssemblyBuilder", ex);
                }
            }

            signal.WaitOne();
        }

        private static void Main(string[] args)
        {
            BuildConfig();
            BuildAssembly();
        }

        private static void CreateFolder(string path)
        {
            var isExists = Directory.Exists(path);

            if (!isExists)
                Directory.CreateDirectory(path);
        }
    }
}