using System;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Facts
{
    public class FactServerCheckIn : ServerScopedFact
    {
        private readonly IQueue<CheckInMessage> _checkInQueue;
        private readonly IDeltaLogger _logger;

        public FactServerCheckIn(IQueue<CheckInMessage> checkInQueue, IDeltaLogger logger)
        {
            _checkInQueue = checkInQueue;
            _logger = logger;
        }

        private FactServerCheckIn() {}

        [SqlAzureRetry]
        public void CreateCheckInFactRow(Guid tenantId, Guid serverId, DateTime timestamp)
        {
            using (var ctx = new DeltaDwContext())
            {
                var dateKey = DimDate.GetSurrogateKeyFromTimestamp(timestamp);
                var timeKey = DimTime.GetSurrogateKeyFromTimestamp(timestamp);
                var tenantKey = DimTenant.GetSurrogateKeyFromNaturalKey(tenantId, timestamp);
                var serverKey = DimServer.GetSurrogateKeyFromNaturalKey(serverId, timestamp);
                var customerKey = DimCustomer.GetSurrogateKeyFromServerSurrogateKey(serverKey, timestamp);
                var customerId = DimCustomer.GetNaturalKeyFromServerSurrogateKey(serverKey, timestamp);

                var fact = new FactServerCheckIn
                               {
                                   DateKey = dateKey,
                                   TimeKey = timeKey,
                                   CustomerKey = customerKey,
                                   CustomerId = customerId,
                                   TenantKey = tenantKey,
                                   TenantId = tenantId,
                                   ServerKey = serverKey,
                                   ServerId = serverId
                               };

                ctx.FactServerCheckIns.Add(fact);
                ctx.SaveChanges();
            }
        }

        public void Update()
        {
            try
            {
                var message = _checkInQueue.GetMessage();

                while (message != null)
                {
                    try
                    {
                        CreateCheckInFactRow(message.TenantId, message.ServerId, message.Timestamp);

                        _checkInQueue.DeleteMessage(message);
                        message = _checkInQueue.GetMessage();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Error in CheckInFact::Update", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error reading messages from Queue in CheckInFact::Update", ex);
            }
        }
    }
}