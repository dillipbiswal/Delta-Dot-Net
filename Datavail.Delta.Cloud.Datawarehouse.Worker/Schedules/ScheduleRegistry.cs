using System;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Facts;
using Datavail.Delta.Infrastructure.Logging;
using FluentScheduler;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Schedules
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
                var tenantDimensionUpdater = _container.Resolve<DimTenant>();
                Schedule(tenantDimensionUpdater.Update).ToRunNow().AndEvery(1).Minutes();

                var customerDimensionUpdater = _container.Resolve<DimCustomer>();
                Schedule(customerDimensionUpdater.Update).ToRunNow().AndEvery(1).Minutes();

                var serverDimensionUpdater = _container.Resolve<DimServer>();
                Schedule(serverDimensionUpdater.Update).ToRunNow().AndEvery(1).Minutes();

                var instanceDimensionUpdater = _container.Resolve<DimInstance>();
                Schedule(instanceDimensionUpdater.Update).ToRunNow().AndEvery(1).Minutes();

                var databaseDimensionUpdater = _container.Resolve<DimDatabase>();
                Schedule(databaseDimensionUpdater.Update).ToRunNow().AndEvery(1).Minutes();

                var sqlAgentJobDimensionUpdater = _container.Resolve<DimSqlAgentJob>();
                Schedule(sqlAgentJobDimensionUpdater.Update).ToRunNow().AndEvery(1).Minutes();

                var checkInFactUpdater = _container.Resolve<FactServerCheckIn>();
                Schedule(checkInFactUpdater.Update).ToRunNow().AndEvery(3).Minutes();

                var factRunner = _container.Resolve<FactRunner>();
                Schedule(factRunner.Update).ToRunNow().AndEvery(1).Minutes();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled exception in ScheduleRegistry::BuildSchedules", ex);
            }
        }
    }
}
