using System;
using System.Collections.Generic;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Application.Interface
{
    public interface ITenantService
    {
        void Create(string name);
        void UpdateStatus(Guid serverId, Status status);
        IEnumerable<Tenant> GetTenantList();

        string GetAlertThresholds(Guid tenantId);
    }
}