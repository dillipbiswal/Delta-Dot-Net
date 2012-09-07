using System;
using System.Threading;
using Datavail.Delta.Agent.Scheduler;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using FluentScheduler;

namespace Datavail.Delta.Infrastructure.Agent.Schedules
{
    public class Scheduler
    {
        private readonly IConfigLoader _configLoader;
        private readonly ICommon _common;
        private readonly IDeltaLogger _logger;

        public Scheduler()
        {
            try
            {
                _configLoader = new ConfigFileLoader();
                _common = new Common.Common();
                _logger = new DeltaLogger();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in Scheduler()", ex);
            }
        }

        public void Execute(WaitHandle waitHandle)
        {
            try
            {
                TaskManager.Initialize(new ScheduleRegistry(_configLoader, _common));

                //Block the thread until the WaitHandle is set. Once set, stop the TaskManager so we can exit gracefully.
                waitHandle.WaitOne();
                TaskManager.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in Scheduler.Execute()", ex);
            }
        }
    }
}