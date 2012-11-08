using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Queues;
using Datavail.Delta.Infrastructure.Queues.Messages;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Repository.Interface;
using Datavail.Delta.RestServices.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web.Http;

namespace Datavail.Delta.RestServices.Controllers
{
    public class ServerController : ApiController
    {
        #region Private Variables
        private readonly IServerRepository _serverRepository;
        private readonly IRepository _repository;

        private readonly IQueue<CheckInMessage> _checkInQueue;
        private readonly IServerService _serverService;
        private readonly IDeltaLogger _logger;
        private readonly IQueue<DataCollectionArchiveMessage> _archiveQueue;
        private readonly IQueue<DataCollectionMessage> _incidentProcessorQueue;
        private readonly IQueue<DataCollectionTestMessage> _testQueue;
        #endregion

        #region Constructor
        public ServerController(IQueue<CheckInMessage> checkInQueue, IQueue<DataCollectionArchiveMessage> archiveQueue, IQueue<DataCollectionMessage> incidentProcessorQueue, IQueue<DataCollectionTestMessage> testQueue, IDeltaLogger deltaLogger, IServerService serverService, IServerRepository serverRepository, IRepository repository)
        {
            _repository = repository;
            _serverRepository = serverRepository;

            _checkInQueue = checkInQueue;
            _serverService = serverService;
            _logger = deltaLogger;
            _archiveQueue = archiveQueue;
            _incidentProcessorQueue = incidentProcessorQueue;
            _testQueue = testQueue;
            
        }
        #endregion

        #region CRUD
        #endregion

        [HttpPost]
        public HttpResponseMessage CheckIn(Guid id, CheckInModel model)
        {
            if (model.TenantId == Guid.Empty || id == Guid.Empty || string.IsNullOrEmpty(model.Hostname) || string.IsNullOrEmpty(model.IpAddress))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var msg = new CheckInMessage { Hostname = model.Hostname, IpAddress = model.IpAddress, ServerId = id, TenantId = model.TenantId, Timestamp = model.TimeStamp };

            _checkInQueue.AddMessage(msg);
            _serverService.CheckIn(model.TenantId, id, model.Hostname, model.IpAddress, model.AgentVersion, model.CustomerId);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpGet]
        public HttpResponseMessage GetConfig(Guid id)
        {
            try
            {
                var config = _serverService.GetConfig(id);
                var model = new ConfigModel
                    {
                        Configuration = config,
                        GeneratingServer = Environment.MachineName,
                        Timestamp = DateTime.UtcNow
                    };

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetConfig (" + id + ")", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetAssemblyList(Guid id)
        {
            try
            {
                var model = new AssemblyListModel
                {
                    GeneratingServer = Environment.MachineName,
                    Timestamp = DateTime.UtcNow
                };

                var assemblies = _serverService.GetAssembliesForServer(id);
                foreach (var assembly in assemblies)
                {
                    var requiredAssembly = new AssemblyModel { AssemblyName = assembly.Key, Version = assembly.Value };
                    model.Assemblies.Add(requiredAssembly);
                }

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetAssemblyList(" + id + ")", ex);
                return null;
            }
        }

        [HttpPost]
        public HttpResponseMessage PostData(Guid id, PostDataModel dataModel)
        {
            try
            {
                if(string.IsNullOrEmpty(dataModel.Data))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Contents cannot be empty");
                }

                var ignoreExpression = WebConfigurationManager.AppSettings["PluginDataIgnoreExpression"] ?? "^a";
                var testDivertExpression = WebConfigurationManager.AppSettings["PluginDataDivertToTestQueueExpression"] ?? "^a";

                if (!Regex.IsMatch(dataModel.Data, ignoreExpression))
                {
                    var msg = new DataCollectionMessage { Data = dataModel.Data, Hostname = dataModel.Hostname, IpAddress = dataModel.IpAddress, ServerId = id, TenantId = dataModel.TenantId, Timestamp = dataModel.Timestamp };
                    _incidentProcessorQueue.AddMessage(msg);

                    var archivemsg = new DataCollectionArchiveMessage { Data = dataModel.Data, Hostname = dataModel.Hostname, IpAddress = dataModel.IpAddress, ServerId = id, TenantId = dataModel.TenantId, Timestamp = dataModel.Timestamp };
                    _archiveQueue.AddMessage(archivemsg);
                }

                if (Regex.IsMatch(dataModel.Data, testDivertExpression))
                {
                    var testmsg = new DataCollectionTestMessage { Data = dataModel.Data, Hostname = dataModel.Hostname, IpAddress = dataModel.IpAddress, ServerId = id, TenantId = dataModel.TenantId, Timestamp = dataModel.Timestamp };
                    _testQueue.AddMessage(testmsg);
                }

                return Request.CreateResponse(HttpStatusCode.OK, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GetAssemblyList(" + id + ")", ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}