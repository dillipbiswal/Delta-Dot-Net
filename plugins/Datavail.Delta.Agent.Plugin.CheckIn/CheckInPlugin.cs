using System;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;


namespace Datavail.Delta.Agent.Plugin.CheckIn
{
    public class CheckInPlugin : IPlugin
    {
        private readonly ICommon _common;
        private readonly ICheckInService _checkInService;
        private readonly IDeltaLogger _logger;
        private readonly IDataQueuer _dataQueuer;

        private string _output;

        public CheckInPlugin()
        {
            _common = new Common();
            _checkInService = new CheckInService();
            _logger = new DeltaLogger();
        }

        public CheckInPlugin(ICommon common, ICheckInService checkInService, IDeltaLogger logger)
        {
            _common = common;
            _checkInService = checkInService;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, String.Format("Check-In PlugIn Executed. MetricInstanceId: {0} Label: {1} (Data: {2})", metricInstance, label, data));

            try
            {
                var tenant = _common.GetTenantId();
                var server = _common.GetServerId();
                var customer = _common.GetCustomerId();
                var agentVersion = _common.GetAgentVersion();
                var hostname = _common.GetHostname();
                var ipaddress = _common.GetIpAddress();

                _checkInService.CheckIn(tenant, server, hostname, ipaddress, agentVersion, customer);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in Check-In Plugin", ex); ;
                try
                {
                    _output = _logger.BuildErrorOutput("CheckInPlugin", "Execute", metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }
        }
    }
}
