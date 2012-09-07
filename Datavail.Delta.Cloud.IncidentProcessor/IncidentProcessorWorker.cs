using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using AutoMapper;
using Datavail.Delta.Application.IncidentProcessor;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Azure;
using Datavail.Delta.Infrastructure.Azure.QueueMessage;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Infrastructure.Util;
using Datavail.Framework.Azure.Cache;
using Datavail.Framework.Azure.Queue;
using Datavail.Framework.Azure.WorkerRole;

namespace Datavail.Delta.Cloud.IncidentProcessor
{
    public class IncidentProcessorWorker : WorkerEntryPoint
    {
        private readonly IAzureCache _cache;
        private readonly IIncidentService _incidentService;
        private readonly IAzureQueue<DataCollectionMessageWithError> _errorQueue;
        private readonly IAzureQueue<DataCollectionMessage> _incidentQueue;
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;
        private List<Type> _ruleClasses;
        private readonly IServerService _serverService;
        private readonly IServiceDesk _serviceDesk;

        //private DataCollectionMessage _message;
        private bool _errorOccurred;

        private readonly PerformanceCounter _perfCounterIncidentProcessorMessagesProcessed;
        private readonly PerformanceCounter _perfCounterIncidentProcessorQueueDepth;
        private readonly PerformanceCounter _perfCounterIncidentProcessorIncidentsOpened;

        public IncidentProcessorWorker(IAzureCache cache, IDeltaLogger logger, IIncidentService incidentService, IAzureQueue<DataCollectionMessageWithError> errorQueue, IAzureQueue<DataCollectionMessage> incidentQueue, IRepository repository, IServerService serverService, IServiceDesk serviceDesk)
        {
            _cache = cache;
            _logger = logger;
            _incidentService = incidentService;
            _errorQueue = errorQueue;
            _incidentQueue = incidentQueue;
            _repository = repository;
            _serverService = serverService;
            _serviceDesk = serviceDesk;

            _perfCounterIncidentProcessorMessagesProcessed = new PerformanceCounter(AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorMessagesProcessed, string.Empty, false);
            _perfCounterIncidentProcessorQueueDepth = new PerformanceCounter(AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorQueueDepth, string.Empty, false);
            _perfCounterIncidentProcessorIncidentsOpened = new PerformanceCounter(AzureConstants.PerfCounters.CountersCategory, AzureConstants.PerfCounters.IncidentProcessorIncidentsOpened, string.Empty, false);
        }

        public override void Run()
        {
            try
            {
                //Get a list of all of the rules classes from the Datavail.Delta.Application assembly
                _ruleClasses = Assembly.GetAssembly(typeof(IncidentProcessorRule)).GetTypes().Where(r => r.GetInterfaces().Contains(typeof(IIncidentProcessorRule)) && r.IsAbstract == false).ToList();

                while (true)
                {
                    try
                    {
                        var messages = _incidentQueue.GetMessages(32, TimeSpan.FromSeconds(300)).ToArray();
                        if (messages.Any())
                        {
                            foreach (var message in messages)
                            {
                                #region Main for/each loop

                                if (message != null)
                                {
                                    Guard.IsNotNull(message.Data, "Message.Data cannot be null");
                                    var xml = XDocument.Parse(message.Data);

                                    //Instantiate each rule class with the message data
                                    var param = new object[] { _cache, _incidentService, xml, _serverService };
                                    var rules = new List<IIncidentProcessorRule>();

                                    foreach (var ruleClass in _ruleClasses)
                                    {
                                        try
                                        {
                                            rules.Add(Activator.CreateInstance(ruleClass, param) as IIncidentProcessorRule);
                                        }
                                        catch (Exception ex)
                                        {
                                            _errorOccurred = true;
                                            HandleException(message, ex, "Error Creating Rule Class (" + ruleClass.Name + ")");
                                        }
                                    }

                                    if (!_errorOccurred)
                                    {
                                        foreach (var rule in rules.Where(r => r.IsMatch() && !IsInMaintenanceMode(r)))
                                        {
                                            var metricInstance = _repository.GetByKey<MetricInstance>(rule.MetricInstance.Id);
                                            var ticket = _incidentService.OpenIncident(rule.IncidentMesage, metricInstance, rule.IncidentPriority, rule.IncidentSummary);
                                            _perfCounterIncidentProcessorIncidentsOpened.Increment();
                                        }

                                        _incidentQueue.BeginDeleteMessage(message, ia => _incidentQueue.EndDeleteMessage(ia), null);
                                        _perfCounterIncidentProcessorMessagesProcessed.Increment();
                                        _perfCounterIncidentProcessorQueueDepth.RawValue = _incidentQueue.GetApproximateMessageCount();
                                    }
                                }

                                Thread.Sleep(TimeSpan.FromSeconds(3));

                                #endregion
                            }
                        }
                        else
                        {
                            Trace.WriteLine("No messages in queue. Sleeping for 10 seconds.");
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                    } //  try/catch inside while(true)
                    catch (Exception ex)
                    {
                        try { HandleException(null, ex, string.Empty); }
                        catch (Exception) { _logger.LogUnhandledException("Unhandled Exception", ex); }
                    }
                } // while(true)
            } // main try/catch
            catch (Exception ex)
            {
                try { HandleException(null, ex, string.Empty); }
                catch (Exception) { _logger.LogUnhandledException("Unhandled Exception", ex); }
            }
        }

        private void HandleException(DataCollectionMessage message, Exception ex, string messageText)
        {
            if (ex is ThreadAbortException)
                return;

            if (ex.Message.Contains("The specified message does not exist"))
                return;

            if (ex is WebException)
            {
                _logger.LogUnhandledException("Error Connecting to ConnectWise Web Service", ex);
                _logger.LogSpecificError(WellKnownWebServicesMessages.UnhandledException, "Error reaching ConnectWise");
                return;
            }

            if (ex.Message.Contains("The underlying provider failed on Open") || ex.Message.Contains("The underlying provider failed on Open"))
            {
                _logger.LogSpecificError(WellKnownWebServicesMessages.UnhandledException, "SQL Azure Error");
                Thread.Sleep(TimeSpan.FromSeconds(10));
                return;
            }

            if (ex.Message.Contains("The underlying connection was closed"))
            {
                _logger.LogSpecificError(WellKnownWebServicesMessages.UnhandledException, "Web Exception");
                return;
            }

            if (message != null)
                if (messageText != null && !string.IsNullOrEmpty(messageText))
                {
                    MoveMessageToErrorQueueOnException(message, ex, messageText);
                }
                else
                {
                    MoveMessageToErrorQueueOnException(message, ex);
                }
        }

        private void MoveMessageToErrorQueueOnException(DataCollectionMessage message, Exception ex, string messageText = "Unhandled Exception")
        {
            if (ex.Message.ToLower().Contains("timeout") || (ex.InnerException != null && ex.InnerException.Message.ToLower().Contains("timeout")))
            {
                _logger.LogUnhandledException("SQL Azure Timeout", ex);
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
            else
            {
                try
                {
                    Mapper.CreateMap<DataCollectionMessage, DataCollectionMessageWithError>();
                    var messageWithError = Mapper.Map<DataCollectionMessage, DataCollectionMessageWithError>(message);
                    messageWithError.ExceptionMessage = ex.ToString();

                    _errorQueue.AddMessage(messageWithError);
                    _incidentQueue.DeleteMessage(message);
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch (Exception)
                // ReSharper restore EmptyGeneralCatchClause
                {
                    //Swallow the error if we can't create a new message and move it to the error queue
                }
                finally
                {
                    _logger.LogUnhandledException(messageText, ex);
                }
            }
        }

        private bool IsInMaintenanceMode(IIncidentProcessorRule rule)
        {
            var server = _repository.GetByKey<Server>(rule.ServerId);
            var metricInstance = _repository.GetByKey<MetricInstance>(rule.MetricInstance.Id);

            //Metric Instance
            if (metricInstance.Status.Enum == Status.InMaintenance)
                return true;

            //Server
            if (server.Status.Enum == Status.InMaintenance)
                return true;

            //ServerGroup
            if (server.ServerGroups != null && server.ServerGroups.Any(serverGroup => serverGroup.Status == Status.InMaintenance))
            {
                return true;
            }

            //Customer
            if (server.Customer != null && server.Customer.Status == Status.InMaintenance)
                return true;

            //Tenant
            if (server.Tenant.Status.Enum == Status.InMaintenance)
                return true;

            //Metric
            return metricInstance.Metric.Status == Status.InMaintenance;
        }
    }
}