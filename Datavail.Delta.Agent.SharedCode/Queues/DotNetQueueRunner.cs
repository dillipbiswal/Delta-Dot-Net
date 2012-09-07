using System;
using System.Collections.Concurrent;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Xml.Serialization;
using Datavail.Delta.Infrastructure.Agent.CollectionServiceProxy;
using Datavail.Delta.Infrastructure.Agent.Common;
using Datavail.Delta.Infrastructure.Agent.Logging;
using Datavail.Delta.Infrastructure.Agent.Queues;

namespace Datavail.Delta.Agent.SharedCode.Queues
{
    public class DotNetQueueRunner
    {
        private readonly ICommon _common;
        private readonly IDeltaLogger _logger;
        private readonly BlockingCollection<QueueMessage> _queue;

        public DotNetQueueRunner()
        {
            try
            {
                _common = new Infrastructure.Agent.Common.Common();
                _logger = new DeltaLogger();
                _queue = DotNetDataQueuerFactory.Current;
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception in QueueRunner()", ex);
            }
        }

        public DotNetQueueRunner(ICommon common, IDeltaLogger deltaLogger)
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

            var path = Path.Combine(_common.GetCachePath(), "QueueData.xml");
            
            //Push the serialized messages into the queue
            if (File.Exists(path))
            {
                var deserializer = new XmlSerializer(_queue.GetType());
                var tr = new StreamReader(path);
                var tempQueue = (BlockingCollection<QueueMessage>)deserializer.Deserialize(tr);
                tr.Close();

                var storedMsg = tempQueue.Take();
                while (storedMsg!=null)
                {
                    _queue.Add(storedMsg);
                    storedMsg = tempQueue.Take();
                }

                File.Delete(path);
            }

            while (true)
            {
                try
                {
                    _logger.LogDebug("QueueRunner Run Begin");

                    var msg = _queue.Take();
                    while (msg != null)
                    {

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

                        if (!result)
                        {
                            _queue.Add(msg);
                        }

                        msg = _queue.Take();
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

                    try
                    {
                        if (_queue.Count > 0)
                        {
                            var serializer = new XmlSerializer(_queue.GetType());
                            var tw = new StreamWriter(path);
                            serializer.Serialize(tw, _queue);
                            tw.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogUnhandledException("Could not serialize queue", ex);
                    }

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