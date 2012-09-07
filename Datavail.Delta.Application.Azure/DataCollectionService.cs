using System;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Azure.Queue;
using Datavail.Delta.Infrastructure.Azure.Queue.Messages;

namespace Datavail.Delta.Application.Azure
{
    public class DataCollectionService : IDataCollectionService
    {
        private readonly IAzureQueue<DataCollectionMessage> _azureQueue;

        public DataCollectionService(IAzureQueue<DataCollectionMessage> azureQueue)
        {
            _azureQueue = azureQueue;
        }

        public void Upload(Guid serverId, string hostname, string ipAddress, string data)
        {
            var message = new DataCollectionMessage() { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId };
            _azureQueue.AddMessage(message);
        }
    }
}
