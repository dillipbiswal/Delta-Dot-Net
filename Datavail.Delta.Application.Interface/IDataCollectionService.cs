using System;

namespace Datavail.Delta.Application.Interface
{
    public interface IDataCollectionService
    {
        bool Upload(Guid serverId, string hostname, string ipAddress, string data);
    }
}