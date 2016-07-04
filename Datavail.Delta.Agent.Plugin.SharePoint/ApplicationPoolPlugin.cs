using System;
using System.Xml.Linq;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.Web.Administration;

namespace Datavail.Delta.Agent.Plugin.SharePoint
{
    public class ApplicationPoolPlugin : IPlugin
    {
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;

        private Guid _metricInstance;
        private string _label;
        private string _AppPoolName;
        private string _AppPoolStatus;

        private string _output;

        public ApplicationPoolPlugin()
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
            _logger = new DeltaLogger();
        }

        public ApplicationPoolPlugin(IDataQueuer dataQueuer, IDeltaLogger logger)
        {
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        #region Execute Methods
        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("ApplicationPoolPlugin.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}", metricInstance, label, data));
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                ParseData(data);

                try
                {

                    GetAppPoolStatus();
                    BuildExecuteOutput();

                    _dataQueuer.Queue(_output);
                    _logger.LogDebug("Data Queued: " + _output);
                }
                catch (Exception ex1)
                {
                    //                        _logger.LogUnhandledException(string.Format("Unhandled Exception while running DiskPlugin::Execute({0},{1},{2})", metricInstance, label, data), ex1);
                }

            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException(string.Format("Unhandled Exception while running ApplicationPoolPlugin::Execute({0},{1},{2})", metricInstance, label, data), ex);
                try
                {
                    _output = _logger.BuildErrorOutput("ApplicationPoolPlugin", "Execute", _metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }

        }

        private void ParseData(string data)
        {
            var xmlData = XElement.Parse(data);
            Guard.ArgumentNotNullOrEmptyString(xmlData.Attribute("Pool").Value, "Pool", "A valid App Pool must be specified for ApplicationPoolPlugin");

            _AppPoolName = xmlData.Attribute("Pool").Value;

        }

        private void GetAppPoolStatus()
        {
            ServerManager server = new ServerManager();
            ApplicationPoolCollection applicationPools = server.ApplicationPools;
            _AppPoolStatus = "";
            foreach (ApplicationPool pool in applicationPools)
            {
                if (pool.Name.ToLower() == _AppPoolName.ToLower())
                {
                    _AppPoolStatus = pool.State.ToString();
                    break;
                }
            }
        }

        private void BuildExecuteOutput()
        {
            var xml = new XElement("ApplicationPoolPluginOutput",
                new XAttribute("timestamp", DateTime.UtcNow),
                new XAttribute("metricInstanceId", _metricInstance),
                new XAttribute("label", _label),
                new XAttribute("resultCode", 0),
                new XAttribute("resultMessage", string.Empty),
                new XAttribute("product", Environment.OSVersion.Platform),
                new XAttribute("productVersion", Environment.OSVersion.Version),
                new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                new XAttribute("productEdition", string.Empty),
                new XAttribute("AppPoolName", _AppPoolName),
                new XAttribute("AppPoolStatus", _AppPoolStatus));

            _output = xml.ToString();
        }
        #endregion
    }
}