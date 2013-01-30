using System;

namespace Datavail.Delta.Infrastructure.Agent.Common
{
    public static class OsInfo
    {
        public static bool IsRunningOnUnix()
        {
            return Environment.OSVersion.Platform.ToString().ToLower().Contains("unix");
        }
    }
}