using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;


namespace Datavail.Delta.Agent.Updater
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            var controller = new ServiceController("DeltaAgentUpdater");
            controller.Start();
        }

        public override void Uninstall(IDictionary savedState)
        {
            var controller = new ServiceController("DeltaAgentUpdater");
            controller.Stop();

            base.Uninstall(savedState);
        }

        public override void Commit(System.Collections.IDictionary savedState)
        {
            base.Commit(savedState);
        }

        public override void Rollback(System.Collections.IDictionary savedState)
        {
            base.Rollback(savedState);
        }

        private void serviceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
