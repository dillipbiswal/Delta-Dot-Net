using System;
using log4net;
using log4net.Config;

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
    }
}
