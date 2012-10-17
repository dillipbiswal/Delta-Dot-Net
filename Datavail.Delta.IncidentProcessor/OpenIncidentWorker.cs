using System;
using System.Threading;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.IncidentProcessor
{
    public class OpenIncidentWorker : WorkerBase
    {
        private readonly IUnityContainer _container;
        private readonly IDeltaLogger _logger;
        private readonly IQueue<OpenIncidentMessage> _openIncidentQueue;
        private OpenIncidentMessage _message;

        public OpenIncidentWorker(IUnityContainer container, IDeltaLogger logger, IQueue<OpenIncidentMessage> openIncidentQueue)
        {
            _container = container;
            _logger = logger;
            _openIncidentQueue = openIncidentQueue;
        }

        public override void Run()
        {
            while (ServiceStarted)
            {
                try
                {
                    if (_openIncidentQueue != null)
                    {
                        _message = _openIncidentQueue.GetMessage();
                        if (_message != null)
                        {
                            var incidentService = _container.Resolve<IIncidentService>();
                            incidentService.OpenIncident(_message.Body, _message.MetricInstanceId, _message.Priority, _message.Summary, _message.AdditionalData);
                            _openIncidentQueue.DeleteMessage(_message);
                        }
                        else
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogUnhandledException("Error in OpenIncidentWorker::Run", ex);
                    if (_message != null && _openIncidentQueue != null)
                    {
                        _openIncidentQueue.DeleteMessage(_message);
                    }
                }
            }
        }
    }
}
