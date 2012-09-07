using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using Datavail.Delta.Infrastructure.Agent;
using Datavail.Delta.Infrastructure.Agent.Logging;

namespace Datavail.Delta.Agent.Updater
{
    public partial class UpdateService : ServiceBase
    {
        private Thread _workerThread;
        private bool _stopCalled = false;
        private readonly IDeltaLogger _logger;

        public UpdateService()
        {
            _logger = new DeltaLogger();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();

            _logger.LogInformational(WellKnownAgentMesage.AgentStarted, "Agent Updater Started");

            //Give the agent time to do its initial work on startup after install
            Thread.Sleep(TimeSpan.FromSeconds(30));

            _workerThread = new Thread(DoWork);
            _workerThread.Start();
            
        }

        protected override void OnStop()
        {
            _stopCalled = true;

            Thread.Sleep(5000);
            _workerThread.Abort();
            _logger.LogInformational(WellKnownAgentMesage.AgentStarted, "Agent Updater Stopped");
        }

        protected void DoWork()
        {
            while (!_stopCalled)
            {
                try
                {
                    var updater = new Infrastructure.Agent.Updater.UpdateRunner();
                    updater.Execute();
                }
                catch (Exception ex)
                {
                    _logger.LogUnhandledException("Unhandled Exception", ex); ;
                }
            }
        }
    }
}
