using System;

namespace Datavail.Delta.Infrastructure.Logging
{
    public enum WellKnownErrorMessages
    {
        InvalidServerId = 1000,
        AgentStopped = 1001,
        QueueEnpointConnectionFailed = 2500,
        QueueEnpointConnectionFailedAfterRetries = 2599,
        EndpointNotReachable = 4000,
        InformationalMessage = 9000,
        UnhandledException = 9999,
    }

    public interface IDeltaLogger
    {
        void LogUnhandledException(string message, Exception exception);
        void LogSpecificError(WellKnownErrorMessages messageId, string message);
        void LogInformational(WellKnownErrorMessages messageId, string message);
        void LogSpecificError(WellKnownErrorMessages messageId, string message, Exception exception);
        void LogInformational(WellKnownErrorMessages messageId, string message, Exception exception);
        void LogDebug(string message);
    }
}