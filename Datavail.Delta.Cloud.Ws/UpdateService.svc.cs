using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Web.Configuration;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;

namespace Datavail.Delta.Cloud.Ws
{
    public class UpdateService : IUpdateService
    {
        private readonly IServerService _serverService;
        private readonly IDeltaLogger _logger;

        public UpdateService(IDeltaLogger deltaLogger, IServerService serverService)
        {
            _serverService = serverService;
            _logger = deltaLogger;
        }

        public byte[] GetAssembly(Guid serverId, string assembly, string version)
        {
            var validServer = _serverService.ServerExists(serverId);
            if (!validServer)
            {
                _logger.LogDebug("Invalid ServerId specified (" + serverId + ")");
                return null;
            }

            try
            {
                var path = WebConfigurationManager.AppSettings["DeltaAssemblyPath"];

                var filename = String.Format("{0}.{1}.dll", assembly, version);
                var fullpath = Path.Combine(path, filename);

                var fs = File.OpenRead(fullpath);
                try
                {
                    var bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                    fs.Close();
                    return bytes;
                }
                finally
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetAssembly (" + serverId + "," + assembly + "," + version + ")", ex);
                return null;
            }
        }

        public string GetConfig(Guid serverId)
        {
            try
            {
                return _serverService.GetConfig(serverId);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetConfig (" + serverId + ")", ex);
                return null;
            }
        }

        public Dictionary<string, string> GetAssemblyList(Guid serverId)
        {
            try
            {
                return _serverService.GetAssembliesForServer(serverId);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetAssemblyList(" + serverId + ")", ex);
                return null;
            }
        }
    }
}