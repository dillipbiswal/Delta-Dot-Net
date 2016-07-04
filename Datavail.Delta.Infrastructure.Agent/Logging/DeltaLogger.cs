using System;
using log4net;
using log4net.Config;
using System.Xml.Linq;

namespace Datavail.Delta.Infrastructure.Agent.Logging
{
    public class DeltaLogger : IDeltaLogger
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(DeltaLogger));

        static DeltaLogger()
        {
            XmlConfigurator.Configure();
        }

        public void LogUnhandledException(string message, Exception exception)
        {
            if (_log.IsErrorEnabled)
                _log.Error(message, exception);
        }

        public void LogSpecificError(WellKnownAgentMesage mesageId, string message)
        {
            if (_log.IsErrorEnabled)
                _log.Error(message);
        }

        public void LogInformational(WellKnownAgentMesage messageId, string message)
        {
            if (_log.IsInfoEnabled)
                _log.Info(message);
        }

        public void LogDebug(string message)
        {
            if (_log.IsDebugEnabled)
                _log.Debug(message);
        }

        public string BuildErrorOutput(string objectName, string methodName, Guid _metricInstance, string ex)
        {
            var xml = new XElement("AgentErrorOutput",
                                   new XAttribute("timestamp", DateTime.UtcNow),
                                   new XAttribute("metricInstanceId", _metricInstance),
                                   new XAttribute("resultMessage", string.Empty),
                                   new XAttribute("product", Environment.OSVersion.Platform),
                                   new XAttribute("productVersion", Environment.OSVersion.Version),
                                   new XAttribute("productLevel", Environment.OSVersion.ServicePack),
                                   new XAttribute("productEdition", string.Empty),
                                   new XAttribute("ObjectName", objectName.ToString()),
                                   new XAttribute("MethodName", methodName.ToString()),
                                   new XAttribute("ErrorMessage", ex.ToString()));

            return xml.ToString();
        }
    }
}
