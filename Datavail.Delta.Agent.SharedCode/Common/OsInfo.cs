using System;

namespace Datavail.Delta.Agent.SharedCode.Common
{
    public static class OsInfo
    {
        public static bool IsRunningOnUnix()
        {
            return Environment.OSVersion.Platform.ToString().ToLower().Contains("unix");
        }
    }
}