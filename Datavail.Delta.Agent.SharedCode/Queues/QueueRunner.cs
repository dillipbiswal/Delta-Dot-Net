using System;
using System.ServiceModel;
using System.Threading;
using Datavail.Delta.Agent.SharedCode.Common;
using Datavail.Delta.Agent.SharedCode.Logging;
using Datavail.Delta.Infrastructure.Agent.CollectionServiceProxy;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public class QueueRunner
    {
        private readonly ICommon _common;
        private readonly IDeltaLogger _logger;

        public QueueRunner()
        {
            try
            {
                _common = new Infrastructure.Agent.Common.Common();
                _logger = new DeltaLogger();
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in QueueRunner()", ex);
            }
        }

        public QueueRunner(ICommon common, IDeltaLogger deltaLogger)
        {
            try
            {
                _common = common;
                _logger = deltaLogger;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in QueueRunner(ICommon common, IDeltaLogger deltaLogger)", ex);
            }
        }

        public void Execute(WaitHandle waitHandle)
        {
            var tenantId = _common.GetTenantId();
            var serverId = _common.GetServerId();
            var hostname = _common.GetHostname();
            var ipaddress = _common.GetIpAddress();


            while (true)
            {
                try
                {
                    _logger.LogDebug("QueueRunner Run Begin");
                    using (var queue = QueueFactory.Current)
                    using (var session = queue.OpenSession())
                    {
                        var data = session.Dequeue();
                        while (data != null)
                        {
                            var msg = QueueMessage.FromByteArray(data);

                            var result = false;
                            try
                            {
                                _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Posting " + msg.Data);
                                using (var collectionService = new CollectionServiceClient())
                                {
                                    result = collectionService.PostCollection(msg.Timestamp, tenantId, serverId, hostname, ipaddress, msg.Data);
                                    collectionService.Close();
                                    _common.SetBackoffTimer(0);
                                }

                            }
                            catch (EndpointNotFoundException)
                            {
                                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Collection WebService is unreachable");
                                DoBackOff();
                            }
                            catch (CommunicationException)
                            {
                                _logger.LogSpecificError(WellKnownAgentMesage.EndpointNotReachable, "The Collection WebService is unreachable");
                                DoBackOff();
                            }
                            catch (ThreadAbortException)
                            {
                                //Don't log anything if we're shutting down
                            }
                            catch (Exception ex)
                            {
                                _logger.LogUnhandledException("Unhandled Exception in QueueRunner.Execute()", ex);
                                DoBackOff();
                            }

                            if (result)
                            {
                                session.Flush();
                            }

                            data = session.Dequeue();
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogUnhandledException("Unhandled Exception in Scheduler.Execute(WaitHandle waitHandle)", ex);
                }

                //Wait to see if we're cancelled, if not loop again   
                var cancelled = waitHandle.WaitOne(100);
                if (cancelled)
                {
                    _logger.LogDebug("Cancel WaitHandle Received in QueueRunner");
                    break;
                }

                _logger.LogDebug("QueueRunner Run End");
            }
        }

        private void DoBackOff()
        {
            //If there is an error communicating with the service, don't fill the error logs.
            //backoff 30 seconds for each loop until we've reached 10 minutes. Then reset to 1 minute. 
            var backoffTimer = _common.GetBackoffTimer();

            if (backoffTimer >= 600)
            {
                backoffTimer = 60;
                _common.SetBackoffTimer(backoffTimer);
            }
            else
            {
                backoffTimer = backoffTimer + 30;
                _common.SetBackoffTimer(backoffTimer);
            }

            _logger.LogInformational(WellKnownAgentMesage.InformationalMessage, "Waiting " + backoffTimer + " seconds before retrying posting queued data");
            Thread.Sleep(TimeSpan.FromSeconds(backoffTimer));
        }
    }
}