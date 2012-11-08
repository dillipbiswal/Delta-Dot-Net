using System;
using log4net;
using log4net.Config;
using log4net.Core;

namespace Datavail.Delta.Infrastructure.Logging
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

        public void LogSpecificError(WellKnownErrorMessages messageId, string message)
        {
            if (_log.IsErrorEnabled)
                _log.Error(string.Format("Error {0}: {1}", (int)messageId, message));
        }

        public void LogInformational(WellKnownErrorMessages messageId, string message)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Error {0}: {1}", (int)messageId, message));
        }

        public void LogDebug(string message)
        {
            if (_log.IsDebugEnabled)
                _log.Debug(message);
        }


        public void LogSpecificError(WellKnownErrorMessages messageId, string message, Exception exception)
        {
            if (_log.IsErrorEnabled)
                _log.Error(string.Format("Error {0}: {1}", (int)messageId, message), exception);
        }

        public void LogInformational(WellKnownErrorMessages messageId, string message, Exception exception)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format("Error {0}: {1}", (int)messageId, message), exception);
        }
    }
}
