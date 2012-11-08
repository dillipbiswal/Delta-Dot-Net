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

    public class TenantsController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static TenantsController()
        {
            Mapper.CreateMap<TenantModel, Tenant>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId));

            Mapper.CreateMap<Tenant, TenantModel>()
                   .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
                   .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status));
        }

        public TenantsController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/")]
        public HttpResponseMessage Post([FromBody]TenantModel model)
        {
            try
            {
                var tenant = Tenant.NewTenant(model.Name);
                Mapper.Map(model, tenant);

                _repository.Add(tenant);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<TenantModel>(tenant);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in POST /tenants", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/tenants")]
        public HttpResponseMessage Get()
        {
            try
            {
                var tenantEntities = _repository.GetQuery<Tenant>(s => s.Status != Status.Deleted).OrderBy(s => s.Name);
                var tenants = Mapper.Map<IEnumerable<TenantModel>>(tenantEntities);

                return Request.CreateResponse(HttpStatusCode.OK, tenants);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{id}")]
        public HttpResponseMessage Get(Guid id)
        {
            try
            {
                var tenant = _repository.GetByKey<Tenant>(id);
                var model = Mapper.Map<TenantModel>(tenant);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/tenants/{id}")]
        public HttpResponseMessage Put(Guid id, [FromBody]TenantModel model)
        {
            try
            {
                var tenant = _repository.GetByKey<Tenant>(id);
                Mapper.Map(model, tenant);

                _repository.Update(tenant);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<TenantModel>(tenant);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /tenants/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{id}")]
        public HttpResponseMessage Delete(Guid id)
        {
            try
            {
                var tenant = _repository.GetByKey<Tenant>(id);
                tenant.Status = Status.Deleted;

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /tenants/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}