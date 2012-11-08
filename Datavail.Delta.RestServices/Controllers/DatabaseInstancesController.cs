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

    public class DatabaseInstancesController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static DatabaseInstancesController()
        {
            Mapper.CreateMap<DatabaseInstanceModel, DatabaseInstance>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DatabaseVersion, opt => opt.MapFrom(src => src.DatabaseVersionId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId));

            Mapper.CreateMap<DatabaseInstance, DatabaseInstanceModel>()
                .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ServerId, opt => opt.MapFrom(src => src.Server.Id))
                .ForMember(dest => dest.DatabaseVersionId, opt => opt.MapFrom(src => src.DatabaseVersion));
        }

        public DatabaseInstancesController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseinstances")]
        public HttpResponseMessage Post(Guid tenantId, Guid customerId, Guid serverId, [FromBody]DatabaseInstanceModel model)
        {
            try
            {
                var server = _repository.GetByKey<Server>(serverId);

                var instance = DatabaseInstance.NewDatabaseInstance(model.Name, server, model.UseIntegratedSecurity, model.Username, model.Password);
                Mapper.Map(model, instance);

                _repository.Add(instance);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<DatabaseInstanceModel>(instance);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in POST /tenants/" + tenantId + "/customers/" + customerId + "/databaseinstances", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid serverId)
        {
            try
            {
                var databaseInstanceEntities = _repository.GetQuery<DatabaseInstance>(s => s.Server.Id == serverId && s.Status != Status.Deleted).OrderBy(s => s.Name);
                var instances = Mapper.Map<IEnumerable<DatabaseInstanceModel>>(databaseInstanceEntities);

                return Request.CreateResponse(HttpStatusCode.OK, instances);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants" + tenantId + "/customers" + customerId + "/servers/" + serverId + "/databaseInstances", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{id}")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid serverId, Guid id)
        {
            try
            {
                var instance = _repository.GetByKey<DatabaseInstance>(id);
                var model = Mapper.Map<DatabaseInstanceModel>(instance);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/databaseInstances/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{id}")]
        public HttpResponseMessage Put(Guid tenantId, Guid customerId, Guid serverId, Guid id, [FromBody]TenantModel model)
        {
            try
            {
                var instance = _repository.GetByKey<DatabaseInstance>(id);
                Mapper.Map(model, instance);

                _repository.Update(instance);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<DatabaseInstanceModel>(instance);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/databaseInstances/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/databaseInstances/{id}")]
        public HttpResponseMessage Delete(Guid tenantId, Guid customerId, Guid serverId, Guid id)
        {
            try
            {
                var instance = _repository.GetByKey<DatabaseInstance>(id);
                instance.Status = Status.Deleted;

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/databaseInstances/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}