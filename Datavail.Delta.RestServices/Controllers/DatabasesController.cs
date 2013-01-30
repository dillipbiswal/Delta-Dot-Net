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

    public class DatabasesController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static DatabasesController()
        {
            Mapper.CreateMap<DatabaseModel, Database>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId));

            Mapper.CreateMap<Database, DatabaseModel>()
                .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.DatabaseInstanceId, opt => opt.MapFrom(src => src.Instance.Id));
        }

        public DatabasesController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseinstances/{instanceId}/databases")]
        public HttpResponseMessage Post(Guid tenantId, Guid customerId, Guid serverId, Guid instanceId, [FromBody]DatabaseModel model)
        {
            try
            {
                var instance = _repository.GetByKey<DatabaseInstance>(serverId);

                var database = Database.NewDatabase(model.Name, instance);
                Mapper.Map(model, database);

                _repository.Add(database);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<DatabaseModel>(database);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in POST /tenants/" + tenantId + "/customers/" + customerId + "/databaseinstances/" + instanceId + "/databases", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{instanceId}/databases")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid serverId, Guid instanceId)
        {
            try
            {
                var databaseEntities = _repository.GetQuery<Database>(s => s.Instance.Id == instanceId && s.Status != Status.Deleted).OrderBy(s => s.Name);
                var instances = Mapper.Map<IEnumerable<DatabaseModel>>(databaseEntities);

                return Request.CreateResponse(HttpStatusCode.OK, instances);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants" + tenantId + "/customers" + customerId + "/servers/" + serverId + "/databaseInstances/" + instanceId + "/databases", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{instanceId}/databases/{id}")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid serverId, Guid instanceId, Guid id)
        {
            try
            {
                var database = _repository.GetByKey<Database>(id);
                var model = Mapper.Map<DatabaseModel>(database);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/databaseInstances/" + instanceId + "/databases/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{instanceId}/databases/{id}")]
        public HttpResponseMessage Put(Guid tenantId, Guid customerId, Guid serverId, Guid instanceId, Guid id, [FromBody]TenantModel model)
        {
            try
            {
                var database = _repository.GetByKey<Database>(id);
                Mapper.Map(model, database);

                _repository.Update(database);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<DatabaseModel>(database);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/databaseInstances/" + instanceId + "/databases/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{instanceId}/databases/{id}")]
        public HttpResponseMessage Delete(Guid tenantId, Guid customerId, Guid serverId, Guid instanceId, Guid id)
        {
            try
            {
                var database = _repository.GetByKey<Database>(id);
                database.Status = Status.Deleted;

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/databaseInstances/" + instanceId + "/databases/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}