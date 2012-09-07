namespace Datavail.Delta.Agent.Updater
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
            this.deltaAgentUpdaterInstaller = new System.ServiceProcess.ServiceInstaller();
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            // 
            // deltaAgentUpdaterInstaller
            // 
            this.deltaAgentUpdaterInstaller.Description = "This service is responsible for updating the Datavail Delta Service";
            this.deltaAgentUpdaterInstaller.DisplayName = "Datavail Delta Agent Updater";
            this.deltaAgentUpdaterInstaller.ServiceName = "DeltaAgentUpdater";
            this.deltaAgentUpdaterInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            this.serviceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_AfterInstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.deltaAgentUpdaterInstaller,
            this.serviceProcessInstaller});

        }

        #endregion

        public System.ServiceProcess.ServiceInstaller deltaAgentUpdaterInstaller;
        public System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;


    }
}