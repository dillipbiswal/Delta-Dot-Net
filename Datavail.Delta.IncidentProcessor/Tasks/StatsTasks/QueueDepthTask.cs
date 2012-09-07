using System;
using System.Net.Mail;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;

namespace Datavail.Delta.IncidentProcessor.Tasks.StatsTasks
{
    public class QueueDepthTask
    {
        private readonly IQueue<DataCollectionArchiveMessage> _archiveQueue;
        private readonly IQueue<DataCollectionMessage> _incidentProcessorQueue;
        private readonly IDeltaLogger _logger;

        public QueueDepthTask(IQueue<DataCollectionArchiveMessage> archiveQueue, IQueue<DataCollectionMessage> incidentProcessorQueue, IDeltaLogger logger)
        {
            _archiveQueue = archiveQueue;
            _incidentProcessorQueue = incidentProcessorQueue;
            _logger = logger;
        }

        public void Execute()
        {
            try
            {
                var queueCount = _incidentProcessorQueue.GetApproximateMessageCount();
                //var perfCounter = new PerformanceCounter(AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorQueueDepth, string.Empty, false)
                //{
                //    RawValue = queueCount
                //};

                if (DateTime.UtcNow.Minute == 0 || DateTime.UtcNow.Minute == 30)
                {
                    var message = new MailMessage();
                    message.To.Add("matt.calhoun@datavail.com");
                    message.Subject = "Delta 4 QueueDepthTask";
                    message.From = new MailAddress("do-not-reply@datavail.com");
                    message.Body = string.Format("The Incident Processor Queue contains {0} messages at {1}", queueCount, DateTime.UtcNow);
                    var smtp = new SmtpClient("datavail.com.inbound10.mxlogic.net");
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Exception in QueueDepthTask::Execute", ex);
            }
        }
    }
}
