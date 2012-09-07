using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Activation;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Datavail.Delta.WebServices
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
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
                _logger.LogSpecificError(WellKnownWebServicesMessages.InvalidServerId, "Invalid ServerId Specified");
                return null;
            }

            try
            {
                var path = RoleEnvironment.GetConfigurationSettingValue("DeltaPluginBlobPath");

                var filename = String.Format("{0}.{1}.dll", assembly, version);
                var filepath = String.Format("{0}{1}", path, filename);

                var webClient = new WebClient();
                var contents = webClient.DownloadData(filepath);

                return contents;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
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
                _logger.LogUnhandledException("Unhandled Exception", ex);
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
                _logger.LogUnhandledException("Unhandled Exception", ex);
                return null;
            }
        }
    }
}
