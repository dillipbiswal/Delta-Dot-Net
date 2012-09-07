namespace Datavail.Delta.Agent
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.deltaAgentInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            // 
            // deltaAgentInstaller
            // 
            this.deltaAgentInstaller.Description = "This service is responsible for collecting metrics and reporting back to the Data" +
    "vail central servers.";
            this.deltaAgentInstaller.DisplayName = "Datavail Delta Agent";
            this.deltaAgentInstaller.ServiceName = "DeltaAgent";
            this.deltaAgentInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.deltaAgentInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.deltaAgentInstaller_AfterInstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.deltaAgentInstaller});

        }

        #endregion

        public System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
        public System.ServiceProcess.ServiceInstaller deltaAgentInstaller;

    }
}