using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public interface IMaintenanceWindowParent
    {
        Guid Id { get; set; }
        StatusWrapper Status { get; set; }
        IList<MaintenanceWindow> MaintenanceWindows { get; set; }
    }
}