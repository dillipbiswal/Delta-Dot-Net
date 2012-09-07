using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Win32;


namespace Datavail.Delta.Agent
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            Debugger.Launch();
            InitializeComponent();
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            RenameCheckInPlugin();
            WriteAgentVersionRegistryValue();

            if (Context.Parameters.ContainsKey("TENANTID") && !string.IsNullOrEmpty(Context.Parameters["TENANTID"]))
            {
                var tenantId = Context.Parameters["TENANTID"];
                WriteTenantIdRegistryValue(tenantId);
            }
            else
            {
                WriteTenantIdRegistryValue("1A19A18A-846C-49DA-93C1-8948AFDC0151");
            }

            if (Context.Parameters.ContainsKey("CUSTOMERID") && !string.IsNullOrEmpty(Context.Parameters["CUSTOMERID"]))
            {
                var customerId = Context.Parameters["CUSTOMERID"];
                WriteCustomerIdRegistryValue(customerId);
            }
        }

        public override void Commit(System.Collections.IDictionary savedState)
        {
            base.Commit(savedState);
        }

        public override void Rollback(System.Collections.IDictionary savedState)
        {
            base.Rollback(savedState);
        }

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            var controller = new ServiceController("DeltaAgent");
            controller.Stop();
            
            DeletePlugins();
            base.Uninstall(savedState);
        }

        private static void RenameCheckInPlugin()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var oldFile = Path.Combine(path, "plugins", "Datavail.Delta.Agent.Plugin.CheckIn.dll");
            var newFile = Path.Combine(path, "plugins", "Datavail.Delta.Agent.Plugin.CheckIn.4.0.0000.0.dll");
            if (File.Exists(oldFile))
            {
                if (File.Exists(newFile))
                    File.Delete(newFile);

                File.Move(oldFile, newFile);
            }
        }

        private static void WriteAgentVersionRegistryValue()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

            var localSystem = Registry.LocalMachine;
            var key = localSystem.OpenSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree) ?? localSystem.CreateSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);

            key.SetValue("AgentVersion", versionString, RegistryValueKind.String);
        }

        private static void WriteCustomerIdRegistryValue(string value)
        {
            var localSystem = Registry.LocalMachine;
            var key = localSystem.OpenSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree) ?? localSystem.CreateSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);

            key.SetValue("CustomerId", value, RegistryValueKind.String);
        }

        private static void WriteTenantIdRegistryValue(string value)
        {
            var localSystem = Registry.LocalMachine;
            var key = localSystem.OpenSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree) ?? localSystem.CreateSubKey("Software\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);

            key.SetValue("TenantId", value, RegistryValueKind.String);
        }

        private static void DeletePlugins()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pluginsPath = Path.Combine(path, "plugins");

            foreach (var file in Directory.EnumerateFiles(pluginsPath))
            {
                File.Delete(file);
            }           
        }

        private void deltaAgentInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            var controller = new ServiceController("DeltaAgent");
            controller.Start();
        }
    }
}
