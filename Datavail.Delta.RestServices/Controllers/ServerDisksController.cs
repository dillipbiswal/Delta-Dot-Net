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

    public class ServerDisksController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static ServerDisksController()
        {
            Mapper.CreateMap<ServerDiskModel, ServerDisk>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            Mapper.CreateMap<ServerDisk, ServerDiskModel>()
                .ForMember(dest => dest.ClusterId, opt => opt.MapFrom(src => src.Cluster.Id))
                .ForMember(dest => dest.ServerId, opt => opt.MapFrom(src => src.Server.Id));
        }

        public ServerDisksController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/disks")]
        public HttpResponseMessage Post(Guid tenantId, Guid customerId, Guid serverId, [FromBody]ServerDiskModel model)
        {
            try
            {

                var server = _repository.GetByKey<Server>(serverId);
                var cluster = _repository.GetByKey<Cluster>(model.ClusterId);

                var disk = cluster == null
                               ? ServerDisk.NewServerDisk(model.Path, server, model.TotalBytes, model.Label)
                               : ServerDisk.NewServerDisk(model.Path, cluster, model.TotalBytes, model.Label);

                Mapper.Map(model, disk);

                _repository.Add(disk);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<ServerDiskModel>(disk);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in POST /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/disks", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/disks")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid serverId)
        {
            try
            {
                var diskEntities = _repository.GetQuery<ServerDisk>(s => s.Server.Id == serverId).OrderBy(s => s.Label);
                var disks = Mapper.Map<IEnumerable<ServerDiskModel>>(diskEntities);

                return Request.CreateResponse(HttpStatusCode.OK, disks);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants" + tenantId + "/customers" + customerId + "/servers/" + serverId + "/disks", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/disks/{id}")]
        public HttpResponseMessage Get(Guid tenantId, Guid customerId, Guid serverId, Guid id)
        {
            try
            {
                var disk = _repository.GetByKey<ServerDisk>(id);
                var model = Mapper.Map<ServerDiskModel>(disk);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/disks/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/disks/{id}")]
        public HttpResponseMessage Put(Guid tenantId, Guid customerId, Guid serverId, Guid id, [FromBody]TenantModel model)
        {
            try
            {
                var disk = _repository.GetByKey<ServerDisk>(id);
                Mapper.Map(model, disk);

                _repository.Update(disk);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<ServerDiskModel>(disk);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/disks/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region DELETE
        [DELETE("/v41/tenants/{tenantId}/customers/{customerId}/servers/{serverId}/disks/{id}")]
        public HttpResponseMessage Delete(Guid tenantId, Guid customerId, Guid serverId, Guid id)
        {
            try
            {
                var disk = _repository.GetByKey<ServerDisk>(id);
                _repository.Delete(disk);

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /tenants/" + tenantId + "/customers/" + customerId + "/servers/" + serverId + "/disks/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}