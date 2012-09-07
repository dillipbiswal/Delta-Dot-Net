using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Datavail.Delta.Cloud.Ws
{
    [ServiceContract]
    public interface IUpdateService
    {
        [OperationContract]
        byte[] GetAssembly(Guid serverId, string assembly, string version);

        [OperationContract]
        Dictionary<string,string> GetAssemblyList(Guid serverId);

        [OperationContract]
        string GetConfig(Guid serverId);
    }
}
