using System;

namespace Datavail.Delta.Infrastructure.Agent
{
    public interface ICheckInService
    {
        void CheckIn(Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion, Guid? customerId);
    }
}
