using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Cluster;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using System.Management;
using Microsoft.Web.Administration;

namespace Datavail.Delta.Agent.Plugin.SharePoint
{
    public class ApplicationPoolInventoryPlugin : IPlugin
    {
        private readonly IDataQueuer _dataQueuer;
        private readonly IDeltaLogger _logger;

        private Guid _metricInstance;
        private string _label;

        private string _output;

        public ApplicationPoolInventoryPlugin()
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

        public ApplicationPoolInventoryPlugin(IDataQueuer dataQueuer, IDeltaLogger logger)
        {
            _dataQueuer = dataQueuer;
            _logger = logger;
        }

        public void Execute(Guid metricInstance, string label, string data)
        {
            _logger.LogDebug(String.Format("ApplicationPoolPlugIn.Execute called. MetricInstanceId: {0} Label: {1} Data: {2}",
                                           metricInstance, label, data));
            try
            {
                _metricInstance = metricInstance;
                _label = label;

                BuildExecuteOutput();
                _dataQueuer.Queue(_output);
            }
            catch (Exception ex)
            {

                _logger.LogUnhandledException("Unhandled Exception", ex);
                try
                {
                    _output = _logger.BuildErrorOutput("ApplicationPoolInventoryPlugin", "Execute", _metricInstance, ex.ToString());
                    _dataQueuer.Queue(_output);
                }
                catch { }

            }
        }

        private void BuildExecuteOutput()
        {
            var xml = new XElement("ApplicationPoolInventoryPluginOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("label", _label),
                                   new XAttribute("resultCode", 0),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty));

            ServerManager server = new ServerManager();
            ApplicationPoolCollection applicationPools = server.ApplicationPools;
            foreach (ApplicationPool pool in applicationPools)
            {
                //get the AutoStart boolean value
                string appPoolstate = pool.State.ToString();              

                //get the name of the ApplicationPool
                string appPoolName = pool.Name;

                //get the identity type
                ProcessModelIdentityType identityType = pool.ProcessModel.IdentityType;
                
                //get the username for the identity under which the pool runs
                string appPooluserName = pool.ProcessModel.UserName;

                //get the password for the identity under which the pool runs
                string appPoolpassword = pool.ProcessModel.Password;
                var node = new XElement("ApplicationPool",
                        new XAttribute("AppPoolName", appPoolName),                        
                        new XAttribute("identityType", identityType),
                        new XAttribute("AppPooluserName", appPooluserName),
                        new XAttribute("AppPoolPassword", appPoolpassword),
                        new XAttribute("AppPoolStatus", appPoolstate));

                xml.Add(node);
            }
            _output = xml.ToString();
        }
    }
}