using System;
using System.Collections.Generic;
using System.ServiceModel;
using Datavail.Delta.Application.Interface;

namespace Datavail.Delta.WebServices
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
