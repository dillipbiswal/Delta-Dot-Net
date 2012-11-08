using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AttributeRouting.Web.Http;
using AutoMapper;
using Datavail.Delta.Domain;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.RestServices.Models;
using System.Linq;

namespace Datavail.Delta.RestServices.Controllers
{

    public class ServersController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;
        private readonly MetricInstancesController _metricInstanceController;

        #region Constructors
        static ServersController()
        {
            Mapper.CreateMap<ServerModel, Server>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AgentVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastCheckIn, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId));

            Mapper.CreateMap<Server, ServerModel>()
                   .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
                   .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Customer.Id))
                   .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.Tenant.Id));
        }

        public ServersController(IDeltaLogger logger, IRepository repository, MetricInstancesController metricInstanceController)
        {
            _logger = logger;
            _repository = repository;
            _metricInstanceController = metricInstanceController;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/{tenantId}/customers/{customerId}/servers/")]
        public HttpResponseMessage Post(Guid tenantId, Guid customerId, [FromBody]ServerModel model)
        {
            return Request.CreateResponse(HttpStatusCode.BadRequest, "Servers are created automatically by the agent");
        }
        #endregion

        #region READ
        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId)
        {
            try
            {
                var serverEntities = _repository.GetQuery<Server>(s => s.Customer.Id == customerId && s.Status != Status.Deleted).OrderBy(s => s.Hostname);
                var servers = Mapper.Map<IEnumerable<ServerModel>>(serverEntities);

                return Request.CreateResponse(HttpStatusCode.OK, servers);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /customers/" + customerId + "/servers", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/servers/unassigned")]
        public HttpResponseMessage GetUnassigned(Guid tenantId)
        {
            try
            {
                var serverEntities = _repository.GetQuery<Server>(s => s.Status == Status.Unknown).OrderBy(s => s.Hostname);
                var servers = Mapper.Map<IEnumerable<ServerModel>>(serverEntities);

                return Request.CreateResponse(HttpStatusCode.OK, servers);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /servers", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{id}")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid id)
        {
            try
            {
                var server = _repository.GetByKey<Server>(id);
                var model = Mapper.Map<ServerModel>(server);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /customers/" + customerId + "/server/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/tenants/{tenantId}/customers/{customerId}/servers/{id}")]
        public HttpResponseMessage Put(Guid tenantId, Guid customerId, Guid id, [FromBody]ServerModel model)
        {
            try
            {
                var server = _repository.GetByKey<Server>(id);
                Mapper.Map(model, server);

                _repository.Update(server);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<ServerModel>(server);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /customers/" + customerId + "/servers" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [PUT("/v41/tenants/{tenantId}/customers/{customerId}/servers/{id}/activate")]
        public HttpResponseMessage ActivateServer(Guid tenantId, Guid customerId, Guid id, [FromBody]ServerModel model)
        {
            try
            {
                var customer = _repository.GetByKey<Customer>(customerId);
                var server = _repository.GetByKey<Server>(id);

                server.Customer = customer;

                //Add the server to the default server group
                var defaultServerGroup = customer.ServerGroups.FirstOrDefault(x => x.Name == "Default");

                if (defaultServerGroup != null)
                {
                    defaultServerGroup.Servers.Add(server);
                }

                _repository.Update(server);
                _repository.UnitOfWork.SaveChanges();

                AddDefaultServerMetrics(id);

                var returnModel = Mapper.Map<ServerModel>(server);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /customers/" + customerId + "/servers" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{tenantId}/customers/{customerId}/servers/{id}")]
        public HttpResponseMessage Delete(Guid tenantId, Guid customerId, Guid id)
        {
            try
            {
                var server = _repository.GetByKey<Server>(id);
                server.Status = Status.Deleted;

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /customers/" + customerId + "/servers/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        private void AddDefaultServerMetrics(Guid serverId)
        {
            var server = _repository.GetByKey<Server>(serverId);

            //CheckinPlugin
            if (!server.MetricInstances.Any(x => x.Metric.AdapterClass == "CheckInPlugin" && x.Status != Status.Deleted))
            {
                if (!server.IsVirtual)
                {
                    var checkInMetric = _repository.Find<Metric>(x => x.AdapterClass == "CheckInPlugin").OrderBy(x => x.AdapterVersion).LastOrDefault();
                    //SaveMetricInstance(Guid.Empty, checkInMetric.Id, serverId,
                    //                   GetMetricData(checkInMetric.Id, serverId), Status.Active,
                    //                   MetricInstanceParentType.Server);

                }
            }
        }
    }
}
