using System.Configuration;
using System.Net.Mail;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Ninject;
using System;
using System.Threading;

namespace Datavail.Delta.IncidentProcessor
{
    public class OpenIncidentWorker : WorkerBase
    {
        private readonly IKernel _kernel;
        private readonly IDeltaLogger _logger;
        private readonly IQueue<OpenIncidentMessage> _openIncidentQueue;
        private OpenIncidentMessage _message;

        private readonly bool _divertCheckInsToEmail;
        private readonly string _checkInMailerFrom;
        private readonly string _checkInSendTo;
        private readonly string _checkInMailerHost;

        public OpenIncidentWorker(IKernel kernel, IDeltaLogger logger, IQueue<OpenIncidentMessage> openIncidentQueue)
        {
            _kernel = kernel;
            _logger = logger;
            _openIncidentQueue = openIncidentQueue;

            try
            {
                bool flag;
                flag = bool.TryParse(ConfigurationManager.AppSettings["DivertCheckInsToEmail"],
                    out _divertCheckInsToEmail);


                if (_divertCheckInsToEmail)
                {
                    _checkInMailerFrom = ConfigurationManager.AppSettings["MailerFrom"];
                    _checkInSendTo = ConfigurationManager.AppSettings["CheckInSendTo"];
                    _checkInMailerHost = ConfigurationManager.AppSettings["MailerHost"];
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in OpenIncidentWorker::Ctor", ex);
            }
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
                            if (_divertCheckInsToEmail && _message.Body.Contains("Last Check In threshold breach"))
                            {
                                _logger.LogDebug(string.Format("Sending Check-In Alert via e-mail\n\nFrom: {0}\nTo: {1}\nSubject: {2}\nBody: {3}", _checkInMailerFrom, _checkInSendTo, _message.Summary, _message.Body));
                                var mailer = new SmtpClient(_checkInMailerHost, 25);
                                var message = new MailMessage(_checkInMailerFrom, _checkInSendTo, _message.Summary, _message.Body);

                                mailer.Send(message);
                            }
                            else
                            {
                                var incidentService = _kernel.Get<IIncidentService>();
                                incidentService.OpenIncident(_message.Body, _message.MetricInstanceId, _message.Priority, _message.Summary, _message.AdditionalData);
                            }
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