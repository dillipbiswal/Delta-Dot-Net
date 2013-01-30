using System;
using System.Linq;
using System.Net.Mail;
using System.Xml.Linq;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Repository.EfWithMigrations;

namespace Datavail.Delta.BatchProcessor
{
    internal class Program
    {
        private static IDeltaLogger _logger;

        private static void Main(string[] args)
        {
            _logger = new DeltaLogger();
            UpdateClosedTickets();
            UpdateMaintWindows();
            ReportQueueDepth();
        }

        #region Closed Tickets
        private static void UpdateClosedTickets()
        {
            try
            {
                using (var context = new DeltaDbContext())
                {
                    var repository = new IncidentRepository(context, _logger);
                    var openTickets = repository.Find(new Specification<IncidentHistory>(i => i.IncidentNumber != "-1" && i.CloseTimestamp == null)).OrderBy(i => i.IncidentNumber).ToList();
                    var serviceDesk = new ServiceDesk(_logger);

                    foreach (var incidentHistory in openTickets)
                    {
                        var status = serviceDesk.GetIncidentStatus(GetStatusXml(incidentHistory.IncidentNumber));
                        Console.WriteLine(string.Format("Checking Open Ticket {0}. Status is {1}", incidentHistory.IncidentNumber, status));
                        if (status == "Closed" || status == "Canceled" || status == "Resolved")
                        {
                            incidentHistory.CloseTimestamp = DateTime.UtcNow;

                            repository.Update(incidentHistory);
                            repository.UnitOfWork.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Exception in ConnectwiseIncidentUpdater::Execute", ex);
            }
        }

        private static string GetStatusXml(string incidentNumber)
        {
            var xml = new XDocument(new XDeclaration("1.0", "utf-16,", "yes"), new XElement("GetIncidentStatus", new XAttribute("IncidentNumber", incidentNumber)));

            return xml.ToString();
        }
        #endregion

        #region Maint Windows
        private static void UpdateMaintWindows()
        {
            try
            {
                using (var context = new DeltaDbContext())
                {
                    var repository = new GenericRepository(context, _logger);
                    var windows = repository.GetAll<MaintenanceWindow>().ToList();

                    foreach (var window in windows)
                    {
                        var beginDate = window.BeginDate;
                        var endDate = window.EndDate;

                        //If the window has started, then set the status to InMaint and store the original status
                        if (beginDate <= DateTime.UtcNow && endDate > DateTime.UtcNow)
                        {
                            window.ParentPreviousStatus = window.Parent.Status;
                            window.Parent.Status = Status.InMaintenance;

                            repository.Update(window);
                            repository.UnitOfWork.SaveChanges();
                        }

                        //If we've passed the end date, then set the status back to the original status
                        if (endDate < DateTime.UtcNow && window.Parent.Status != window.ParentPreviousStatus)
                        {
                            window.Parent.Status = window.ParentPreviousStatus;
                            window.ParentPreviousStatus = Status.Active;

                            repository.Update(window);
                            repository.UnitOfWork.SaveChanges();
                        }

                        //Delete all windows older than 30 days
                        if (endDate < DateTime.UtcNow.AddDays(-30))
                        {
                            repository.Delete(window);
                            repository.UnitOfWork.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Exception in UpdateMaintWindows()", ex);
            }
        }
        #endregion

        #region Queue Depth
        private static void ReportQueueDepth()
        {
            try
            {
                var incidentProcessorQueue = new SqlQueue<DataCollectionMessage>(QueueNames.IncidentProcessorQueue);
                var queueCount = incidentProcessorQueue.GetApproximateMessageCount();


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
        #endregion
    }
}
