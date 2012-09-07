using System;
using Datavail.Delta.IncidentProcessor.Tasks.MaintenanceWindows;
using Datavail.Delta.IncidentProcessor.Tasks.ServiceDeskTasks;
using Datavail.Delta.IncidentProcessor.Tasks.StatsTasks;
using Datavail.Delta.Infrastructure.Logging;
using FluentScheduler;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.IncidentProcessor.Tasks.Schedules
{
    public class ScheduleRegistry : Registry
    {
        private readonly IDeltaLogger _logger;
        private readonly IUnityContainer _container;

        public ScheduleRegistry(IUnityContainer container, IDeltaLogger logger)
        {
            _container = container;
            _logger = logger;

            BuildSchedules();
        }

        private void BuildSchedules()
        {
            try
            {
                //Schedule ConnectwiseIncidentUpdater to run
                var incidentUpdater = _container.Resolve<ConnectwiseIncidentUpdater>();
                Schedule(incidentUpdater.Execute).ToRunNow().AndEvery(1).Minutes();

                //Schedule Maint Window Task to run
                var maintWindowTask = _container.Resolve<MaintenanceWindowTask>();
                Schedule(maintWindowTask.Execute).ToRunNow().AndEvery(1).Minutes();

                //Schedule Maint Window Task to run
                var queueDepthTask = _container.Resolve<QueueDepthTask>();
                Schedule(queueDepthTask.Execute).ToRunNow().AndEvery(1).Minutes();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled exception in ScheduleRegistry::BuildSchedules", ex);
            }
        }
    }
}
