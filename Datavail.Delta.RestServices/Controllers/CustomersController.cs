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

    public class CustomersController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static CustomersController()
        {
            Mapper.CreateMap<CustomerModel, Customer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId))
                .ForMember(dest => dest.Tenant, opt => opt.Ignore());

            Mapper.CreateMap<Customer, CustomerModel>()
                    .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
                    .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.Tenant.Id));
        }

        public CustomersController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/{tenantId}/customers/")]
        public HttpResponseMessage Post(Guid tenantId, [FromBody]CustomerModel model)
        {
            try
            {
                var tenant = _repository.GetByKey<Tenant>(tenantId);

                var customer = Customer.NewCustomer(tenant, model.Name);
                Mapper.Map(model, customer);

                _repository.Add(customer);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<CustomerModel>(customer);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /Customer/", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/tenants/{tenantId}/customers/")]
        public HttpResponseMessage Get(Guid tenantId)
        {
            try
            {
                var customerEntities = _repository.GetQuery<Customer>(c => c.Tenant.Id == tenantId && c.Status != Status.Deleted).OrderBy(c => c.Name);
                var customers = Mapper.Map<IEnumerable<CustomerModel>>(customerEntities);

                return Request.CreateResponse(HttpStatusCode.OK, customers);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /Customer/", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/customers/{id}")]
        public HttpResponseMessage Get(Guid tenantId, Guid id)
        {
            try
            {
                var customer = _repository.GetByKey<Customer>(id);
                var model = Mapper.Map<CustomerModel>(customer);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /Customer/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
        
        #region UPDATE
        [PUT("/v41/tenants/{tenantId}/customers/{id}")]
        public HttpResponseMessage Put(Guid tenantId, Guid id, [FromBody]CustomerModel model)
        {
            try
            {
                var customer = _repository.GetByKey<Customer>(id);
                Mapper.Map(model, customer);

                _repository.Update(customer);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<CustomerModel>(customer);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /Customer/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{tenantId}/customers/{id}")]
        public HttpResponseMessage Delete(Guid tenantId, Guid id)
        {
            try
            {
                var customer = _repository.GetByKey<Customer>(id);
                customer.Status = Status.Deleted;

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error DELETE /Customer/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}
