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

    public class ServerGroupsController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static ServerGroupsController()
        {
            Mapper.CreateMap<ServerGroupModel, ServerGroup>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            Mapper.CreateMap<ServerGroup, ServerGroupModel>()
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.Parent.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
        }

        public ServerGroupsController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/{tenantId}/customers/{customerId}/groups")]
        public HttpResponseMessage Post(Guid tenantId, Guid customerId, [FromBody]ServerGroupModel model)
        {
            try
            {

                var customer = _repository.GetByKey<Customer>(customerId);
                var serverGroup = ServerGroup.NewServerGroup(customer, model.Name, model.Priority);


                Mapper.Map(model, serverGroup);

                _repository.Add(serverGroup);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<ServerGroupModel>(serverGroup);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in POST /tenants/" + tenantId + "/customers/" + customerId + "/groups", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/tenants/{tenantId}/customers/{customerId}/groups")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId)
        {
            try
            {
                var serverGroupEntities = _repository.GetQuery<ServerGroup>(s => s.ParentCustomer.Id == customerId).OrderBy(s => s.Name);
                var groups = Mapper.Map<IEnumerable<ServerGroupModel>>(serverGroupEntities);

                return Request.CreateResponse(HttpStatusCode.OK, groups);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants" + tenantId + "/customers" + customerId + "/groups", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/customers/{customerId}/groups/{id}")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid id)
        {
            try
            {
                var group = _repository.GetByKey<ServerGroup>(id);
                var model = Mapper.Map<ServerGroupModel>(group);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants/" + tenantId + "/customers/" + customerId + "/groups/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/tenants/{tenantId}/customers/{customerId}/groups/{id}")]
        public HttpResponseMessage Put(Guid tenantId, Guid customerId, Guid id, [FromBody]TenantModel model)
        {
            try
            {
                var group = _repository.GetByKey<ServerGroup>(id);
                Mapper.Map(model, group);

                _repository.Update(group);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<ServerGroupModel>(group);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /tenants/" + tenantId + "/customers/" + customerId + "/groups/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{tenantId}/customers/{customerId}/groups/{id}")]
        public HttpResponseMessage Delete(Guid tenantId, Guid customerId, Guid id)
        {
            try
            {
                var group = _repository.GetByKey<ServerGroup>(id);
                _repository.Delete(group);

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /tenants/" + tenantId + "/customers/" + customerId + "/groups/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}