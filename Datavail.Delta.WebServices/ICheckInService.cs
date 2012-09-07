using System;
using System.ServiceModel;

namespace Datavail.Delta.WebServices
{
    [ServiceContract]
    public interface ICheckInService
    {
        [OperationContract]
        bool CheckIn(DateTime timestamp, Guid tenantId, Guid serverId, string hostname, string ipAddress, string agentVersion);
    }
}
