using System;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;

namespace Datavail.Delta.Application
{
    public class DataCollectionService : IDataCollectionService
    {
        private readonly IQueue<DataCollectionMessage> _azureQueue;

        public DataCollectionService(IQueue<DataCollectionMessage> azureQueue)
        {
            _azureQueue = azureQueue;
        }

        public bool Upload(Guid serverId, string hostname, string ipAddress, string data)
        {
            var message = new DataCollectionMessage { Data = data, Hostname = hostname, IpAddress = ipAddress, ServerId = serverId };
            _azureQueue.AddMessage(message);

            return true;
        }
    }
}
