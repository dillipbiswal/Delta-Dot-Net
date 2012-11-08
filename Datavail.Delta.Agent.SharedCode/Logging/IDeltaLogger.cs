using System;

namespace Datavail.Delta.Agent.SharedCode.Logging
{
    public enum WellKnownAgentMesage
    {
        AgentStarted = 1000,
        AgentStopped = 1001,
        EndpointNotReachable = 4000,
        InformationalMessage = 9000,
        UnhandledException = 9999,
    }

    public interface IDeltaLogger
    {
        void LogUnhandledException(string message, Exception exception);
        void LogSpecificError(WellKnownAgentMesage mesageId, string message);
        void LogInformational(WellKnownAgentMesage messageId, string message);
        void LogDebug(string message);
    }
}