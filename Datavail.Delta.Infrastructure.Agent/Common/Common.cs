using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Win32;

namespace Datavail.Delta.Infrastructure.Agent.Common
{
    public class Common : ICommon
    {
        public Common()
        {

        }

        public Guid GetServerId()
        {
            var localSystem = Registry.LocalMachine;
            var key = localSystem.OpenSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree) ?? localSystem.CreateSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);

            var serverId = key.GetValue("ServerId");
            if (serverId == null)
            {
                key.SetValue("ServerId", Guid.NewGuid(), RegistryValueKind.String);
            }

            var value = Guid.Parse(key.GetValue("ServerId").ToString());
            return value;
        }

        public Guid GetTenantId()
        {
            var value = string.Empty;
            if (!OsInfo.IsRunningOnUnix())
            {
                var localSystem = Registry.LocalMachine;
                var key = localSystem.OpenSubKey("Software\\Datavail\\Delta");
                value = key.GetValue("TenantId").ToString();
            }
            else
            {
                value = ConfigurationManager.AppSettings["TenantId"];
            }
            return Guid.Parse(value);
        }

        public Guid? GetCustomerId()
        {
            var value = string.Empty;
            if (!OsInfo.IsRunningOnUnix())
            {
                var localSystem = Registry.LocalMachine;
                var key = localSystem.OpenSubKey("Software\\Datavail\\Delta");
                value = key.GetValue("CustomerId", string.Empty).ToString();

                if (string.IsNullOrEmpty(value)) return null;
            }
            else
            {
                value = ConfigurationManager.AppSettings["CustomerId"];
            }
            return Guid.Parse(value);
        }

        public string GetAgentVersion()
        {
            var value = string.Empty;
            if (!OsInfo.IsRunningOnUnix())
            {
                try
                {
                    var localSystem = Registry.LocalMachine;
                    var key = localSystem.OpenSubKey("Software\\Datavail\\Delta");
                    value = key.GetValue("AgentVersion").ToString();
                }
                catch (Exception)
                {
                    value = "Unknown";
                }

            }
            else
            {
                value = ConfigurationManager.AppSettings["AgentVersion"];
            }

            return value;
        }

        public string GetHostname()
        {
            return Environment.MachineName;
        }

        public string GetIpAddress()
        {
            return Dns.GetHostAddresses(Dns.GetHostName()).Where(i => i.AddressFamily == AddressFamily.InterNetwork).Where(i => i.ToString().Contains("169.254") == false).FirstOrDefault().ToString();
        }

        public string GetCachePath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (path != null)
            {
                path = path.Replace("file:\\", "");
                path = path.Replace("file:", "");
                path = Path.Combine(path, "cache");
            }

            return path;
        }

        public string GetPluginPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (path != null)
            {
                path = path.Replace("file:\\", "");
                path = path.Replace("file:", "");
                path = Path.Combine(path, "plugins");
            }

            return path;
        }

        public string GetConfigPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (path != null)
            {
                path = path.Replace("file:\\", "");
                path = path.Replace("file:", "");
            }

            return Path.Combine(new[] { path, "DeltaAgent.xml" });
        }


        public string GetOnDemandConfigPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (path != null)
            {
                path = path.Replace("file:\\", "");
                path = path.Replace("file:", "");
            }

            return Path.Combine(new[] { path, "DeltaOnDemandMetric.xml" });
        }

        public string GetTempPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (path != null)
            {
                path = path.Replace("file:\\", "");
                path = path.Replace("file:", "");
            }

            return Path.Combine(new[] { path, "temp" });
        }

        public int GetUpdaterRunInterval()
        {
            //var localSystem = Registry.LocalMachine;
            //var key = localSystem.OpenSubKey("Software\\Datavail\\Delta");

            //var value = key.GetValue("UpdaterRunInterval").ToString();
            var value = ConfigurationManager.AppSettings["UpdaterRunInterval"];
            return Int32.Parse(value);
        }


        public int GetBackoffTimer()
        {
            var localSystem = Registry.LocalMachine;
            var key = localSystem.OpenSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree) ?? localSystem.CreateSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);

            var serverId = key.GetValue("BackoffTimer");
            if (serverId == null)
            {
                return 0;
            }

            var value = Int32.Parse(key.GetValue("BackoffTimer").ToString());
            return value;
        }

        public void SetBackoffTimer(int timerValue)
        {
            var localSystem = Registry.LocalMachine;
            var key = localSystem.OpenSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree) ?? localSystem.CreateSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);

            key.SetValue("BackoffTimer", timerValue, RegistryValueKind.String);
        }
    }
}
