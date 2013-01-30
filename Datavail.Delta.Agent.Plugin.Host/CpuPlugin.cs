using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.Plugin.Host
{
    public class CpuPlugin : IPlugin
    {
        private readonly ISystemInfo _systemInfo;
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;
        
        private Guid _metricInstance;
        private string _label;

        private string _output;

        public CpuPlugin()
        {
            var common = new Common();
            if (common.GetAgentVersion().Contains("4.0."))
            {
                _dataQueuer = new DataQueuer();
            }
            else
            {
                _dataQueuer = new DotNetDataQueuer();    
            }
            
            _systemInfo = new SystemInfo();
            _logger = new DeltaLogger();
        }

        public CpuPlugin(ISystemInfo systemInfo, IDataQueuer dataQueuer, IDeltaLogger logger)
        {
            _systemInfo = systemInfo;
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        #region Execute Methods

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("CpuPlugIn.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                           metricInstance, label, data));
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                BuildExecuteOutput();

                _dataQueuer.Queue(_output);
                _logger.LogDebug("Data Queued: " + _output);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("Unhandled Exception while running CpuPlugin::Execute({0},{1},{2})", metricInstance, label, data), ex);
            }
        }

        private void BuildExecuteOutput()
        {
            var xml = new XElement("CpuPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("percentageCpuUsed", _systemInfo.GetCpuUtilization()));

            _output = xml.ToString();
        }

        #endregion
    }
}