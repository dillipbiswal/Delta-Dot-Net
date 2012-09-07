using System;
using System.Linq;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.IncidentProcessor.Tasks.MaintenanceWindows
{
    public class MaintenanceWindowTask
    {
        private readonly IRepository _repository;
        private readonly IDeltaLogger _logger;

        public MaintenanceWindowTask(IRepository repository, IDeltaLogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public void Execute()
        {
            try
            {
                var windows = _repository.GetAll<MaintenanceWindow>().ToList();

                foreach (var window in windows)
                {
                    var beginDate = window.BeginDate;
                    var endDate = window.EndDate;

                    //If the window has started, then set the status to InMaint and store the original status
                    if (beginDate <= DateTime.UtcNow && endDate > DateTime.UtcNow)
                    {
                        window.ParentPreviousStatus = window.Parent.Status;
                        window.Parent.Status = Status.InMaintenance;

                        _repository.Update(window);
                        _repository.UnitOfWork.SaveChanges();
                    }

                    //If we've passed the end date, then set the status back to the original status
                    if (endDate < DateTime.UtcNow && window.Parent.Status != window.ParentPreviousStatus)
                    {
                        window.Parent.Status = window.ParentPreviousStatus;
                        window.ParentPreviousStatus = Status.Active;

                        _repository.Update(window);
                        _repository.UnitOfWork.SaveChanges();
                    }

                    //Delete all windows older than 30 days
                    if (endDate < DateTime.UtcNow.AddDays(-30))
                    {
                        _repository.Delete(window);
                        _repository.UnitOfWork.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Exception in MaintenanceWindowTask::Execute", ex);
            }
        }
    }
}