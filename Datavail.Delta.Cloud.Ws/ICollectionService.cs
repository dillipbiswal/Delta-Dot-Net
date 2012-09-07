using System;
using System.ServiceModel;

namespace Datavail.Delta.Cloud.Ws
{
    [ServiceContract]
    public interface ICollectionService
    {
        [OperationContract]
        bool PostCollection(DateTime timestamp, Guid tenantId, Guid serverId, string hostname, string ipAddress, string data);
    }
}
