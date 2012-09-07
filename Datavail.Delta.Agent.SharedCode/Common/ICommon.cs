using System;

namespace Datavail.Delta.Agent.SharedCode.Common
{
    public interface ICommon
    {
        int GetUpdaterRunInterval();
        Guid GetServerId();
        Guid GetTenantId();
        Guid? GetCustomerId();
        string GetAgentVersion();
        string GetHostname();
        string GetIpAddress();
        string GetCachePath();
        string GetPluginPath();
        string GetConfigPath();
        string GetTempPath();

        int GetBackoffTimer();
        void SetBackoffTimer(int timerValue);
    }
}