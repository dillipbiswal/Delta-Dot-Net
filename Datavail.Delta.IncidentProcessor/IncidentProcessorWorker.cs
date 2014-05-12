using System.Configuration;
using AutoMapper;
using Datavail.Delta.Application;
using Datavail.Delta.Application.IncidentProcessor;
using Datavail.Delta.Application.Interface;
using Datavail.Delta.Application.ServiceDesk.ConnectWise;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.EfWithMigrations;
using Datavail.Delta.Repository.Interface;
using Ninject;
using Ninject.Activation.Blocks;
using Ninject.Extensions.ChildKernel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace Datavail.Delta.IncidentProcessor
{

    public class IncidentProcessorWorker : WorkerBase
    {
        private readonly IKernel _kernel;
        private IIncidentService _incidentService;
        private readonly IQueue<DataCollectionMessage> _incidentQueue;
        private readonly IQueue<OpenIncidentMessage> _openIncidentQueue;
        private readonly IQueue<DataCollectionMessageWithError> _errorQueue;
        private readonly IDeltaLogger _logger;
        private IServerRepository _repository;
        private List<Type> _ruleClasses;
        private IServerService _serverService;
        private DataCollectionMessage _message;



        public IncidentProcessorWorker(IKernel kernel, IDeltaLogger logger, IQueue<DataCollectionMessage> incidentQueue, IQueue<OpenIncidentMessage> openIncidentQueue, IQueue<DataCollectionMessageWithError> errorQueue)
        {
            _kernel = kernel;
            _logger = logger;
            _incidentQueue = incidentQueue;
            _openIncidentQueue = openIncidentQueue;
            _errorQueue = errorQueue;
        }

        public override void Run()
        {
            try
            {
                //Get a list of all of the rules classes from the Datavail.Delta.Application assembly
                _ruleClasses = Assembly.GetAssembly(typeof(IncidentProcessorRule)).GetTypes().Where(r => r.GetInterfaces().Contains(typeof(IIncidentProcessorRule)) && r.IsAbstract == false).ToList();

                while (ServiceStarted)
                {
                    try
                    {
                        _message = _incidentQueue.GetMessage();

                        if (_message != null)
                        {
                            if (string.IsNullOrEmpty(_message.Data))
                            {
                                _logger.LogUnhandledException("Message.Data cannot be null", null);
                                _incidentQueue.DeleteMessage(_message);
                                continue;
                            }

                            var xml = XDocument.Parse(_message.Data);

                            var context = new DeltaDbContext();
                            _repository = new ServerRepository(context, _logger);
                            _serverService = new ServerService(_logger, _repository);
                            var incidentRepository = new IncidentRepository(context, _logger);

                            var connectwise = new ServiceDesk(_logger);
                            _incidentService = new IncidentService(connectwise, incidentRepository);

                            //using (var block = new ActivationBlock(childKernel))
                            {
                                //Instantiate each rule class with the message data
                                var param = new object[] { _incidentService, xml, _serverService };
                                var rules = new List<IIncidentProcessorRule>();

                                foreach (var ruleClass in _ruleClasses)
                                {
                                    try
                                    {
                                        rules.Add(Activator.CreateInstance(ruleClass, param) as IIncidentProcessorRule);
                                    }
                                    catch (Exception ex)
                                    {
                                        try
                                        {
                                            Mapper.CreateMap<DataCollectionMessage, DataCollectionMessageWithError>();
                                            var errorMessage = Mapper.Map<DataCollectionMessageWithError>(_message);
                                            errorMessage.ExceptionMessage = ex.Message;

                                            _errorQueue.AddMessage(errorMessage);
                                        }
                                        // ReSharper disable EmptyGeneralCatchClause
                                        catch (Exception)
                                        {
                                            //Swallow the exception if we can't move to the error queue
                                        }
                                        // ReSharper restore EmptyGeneralCatchClause

                                        _logger.LogUnhandledException("Error Creating Rule Class (" + ruleClass.Name + ")", ex);
                                    }
                                }

                                foreach (
                                    var openIncidentMessage in
                                        rules.Where(r => r.IsMatch() && !IsInMaintenanceMode(r)).Select(
                                            rule =>
                                            new OpenIncidentMessage
                                                {
                                                    Body = rule.IncidentMesage,
                                                    MetricInstanceId = rule.MetricInstance.Id,
                                                    Priority = rule.IncidentPriority,
                                                    Summary = rule.IncidentSummary,
                                                    AdditionalData = rule.AdditionalData
                                                }))
                                {
                                    _openIncidentQueue.AddMessage(openIncidentMessage);
                                }
                            }

                            //childKernel.Dispose();
                        }
                        else
                        {
                            Trace.WriteLine(string.Format("No messages in queue. Thread {0} sleeping for 3 seconds.", Thread.CurrentThread.ManagedThreadId));
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                        }

                    }
                    catch (Exception ex)
                    {

                        try
                        {
                            Mapper.CreateMap<DataCollectionMessage, DataCollectionMessageWithError>();
                            var errorMessage = Mapper.Map<DataCollectionMessageWithError>(_message);
                            errorMessage.ExceptionMessage = ex.Message;

                            _errorQueue.AddMessage(errorMessage);
                        }
                        // ReSharper disable EmptyGeneralCatchClause
                        catch (Exception)
                        {
                            //Swallow the exception if we can't move to the error queue
                        }
                        _logger.LogUnhandledException("Unhandled Exception", ex);
                    }
                    finally
                    {
                        if (_message != null)
                        {
                            try
                            {
                                _incidentQueue.DeleteMessage(_message);
                                Trace.WriteLine(string.Format("Deleting Message. Receipt: {0} processed", _message.PopReceipt));
                                Trace.WriteLine("--------------------------------------------------------------------------");
                            }
                            // ReSharper disable EmptyGeneralCatchClause
                            catch (Exception ex)
                            {
                                //Swallow any exceptions here so thread doesn't blow up
                            }
                            // ReSharper restore EmptyGeneralCatchClause
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Unhandled Exception", ex);
            }
        }

        private bool IsInMaintenanceMode(IIncidentProcessorRule rule)
        {
            var server = _repository.GetByKey<Server>(rule.ServerId);
            var metricInstance = _repository.GetByKey<MetricInstance>(rule.MetricInstance.Id);

            //Metric Instance
            if (metricInstance.Status != Status.Active)
                return true;

            //Database
            if (metricInstance.Database != null && metricInstance.Database.Status != Status.Active)
                return true;

            //Database Instance
            if (metricInstance.DatabaseInstance != null && metricInstance.DatabaseInstance.Status != Status.Active)
                return true;

            //Server
            if (server.Status != Status.Active)
                return true;

            //ServerGroup
            if (server.ServerGroups != null && server.ServerGroups.Any(serverGroup => serverGroup.Status == Status.InMaintenance))
            {
                return true;
            }

            //Customer
            if (server.Customer != null && server.Customer.Status != Status.Active)
                return true;

            //Tenant
            if (server.Tenant.Status != Status.Active)
                return true;

            //Metric
            return metricInstance.Metric.Status != Status.Active;
        }
    }
}