using System;
using System.ServiceProcess;
using System.Threading;
using Datavail.Delta.Agent.SharedCode.Queues;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;
using Microsoft.Win32;

namespace Datavail.Delta.Agent
{
    public partial class DeltaAgentService : ServiceBase
    {
        private Thread _schedulerThread;
        private Thread _queueRunnerThread;
        private static readonly EventWaitHandle SchedulerWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static readonly EventWaitHandle QueueRunnerWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static readonly IDeltaLogger Logger = new DeltaLogger();

        public DeltaAgentService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();

            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Datavail\\Delta", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (key != null)
            {
                Guid idGuid;
                var id = Guid.TryParse(key.GetValue("ServerId").ToString(), out idGuid);

                if (idGuid == Guid.Empty)
                {
                    idGuid = Guid.NewGuid();
                    key.SetValue("ServerId", idGuid);
                }
            }

            _queueRunnerThread = new Thread(StartQueueRunner);
            _queueRunnerThread.Start();
            
            _schedulerThread = new Thread(StartScheduler);
            _schedulerThread.Start();

            Logger.LogInformational(WellKnownAgentMesage.AgentStarted, "Agent Started");
        }

        protected override void OnStop()
        {
            try
            {
                SchedulerWaitHandle.Set();
                QueueRunnerWaitHandle.Set();

                _schedulerThread.Join(10000);
                _queueRunnerThread.Join(10000);

                _queueRunnerThread.Abort();
                _schedulerThread.Abort();

                Logger.LogInformational(WellKnownAgentMesage.AgentStopped, "Agent Stopped");

            }
            catch (ThreadAbortException)
            {
            }
        }

        //Delegate to start scheduler
        private static void StartScheduler()
        {
            try
            {
                Logger.LogDebug("StartScheduler() Called");

                var scheduler = new Infrastructure.Agent.Schedules.Scheduler();
                scheduler.Execute(SchedulerWaitHandle);
            }
            catch (ThreadAbortException ex)
            {
                //Swallow if we're shutting down
            }
            catch (Exception ex)
            {
                Logger.LogUnhandledException("Unhandled Exception", ex); ;
            }
        }

        private static void StartQueueRunner()
        {
            try
            {
                Logger.LogDebug("StartQueueRunner() Called");
                var queueRunner = new DotNetQueueRunner();
                queueRunner.Execute(QueueRunnerWaitHandle);
            }
            catch (Exception ex)
            {
                Logger.LogUnhandledException("Unhandled Exception in StartQueueRunner()", ex); ;
            }
        }
    }
}
