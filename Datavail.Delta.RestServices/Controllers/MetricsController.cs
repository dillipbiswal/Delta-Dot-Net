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

    public class MetricsController : ApiController
    {
        private readonly IDeltaLogger _logger;
        private readonly IRepository _repository;

        #region Constructors
        static MetricsController()
        {
            Mapper.CreateMap<MetricModel, Metric>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DatabaseVersion, opt => opt.MapFrom(src => src.DatabaseVersionId))
                .ForMember(dest => dest.MetricThresholdType, opt => opt.MapFrom(src => src.MetricThresholdTypeId))
                .ForMember(dest => dest.MetricType, opt => opt.MapFrom(src => src.MetricTypeId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusId));

            Mapper.CreateMap<Metric, MetricModel>()
                   .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status))
                   .ForMember(dest => dest.DatabaseVersionId, opt => opt.MapFrom(src => src.DatabaseVersion))
                   .ForMember(dest => dest.MetricThresholdTypeId, opt => opt.MapFrom(src => src.MetricThresholdType))
                   .ForMember(dest => dest.MetricTypeId, opt => opt.MapFrom(src => src.MetricType))
                   .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.Status));
        }

        public MetricsController(IDeltaLogger logger, IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        #endregion

        #region CREATE
        [POST("/v41/metrics/")]
        public HttpResponseMessage Post([FromBody]MetricModel model)
        {
            try
            {
                var metric = Metric.NewMetric(model.AdapterAssembly, model.AdapterClass, model.AdapterVersion, model.Name);
                Mapper.Map(model, metric);

                _repository.Add(metric);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<MetricModel>(metric);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in POST /metrics", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region READ
        [GET("/v41/metrics")]
        public HttpResponseMessage Get()
        {
            try
            {
                var metricEntities = _repository.GetQuery<Metric>(s => s.Status != Status.Deleted).OrderBy(s => s.Name);
                var metrics = Mapper.Map<IEnumerable<MetricModel>>(metricEntities);

                return Request.CreateResponse(HttpStatusCode.OK, metrics);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /metrics/", ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [GET("/v41/metrics/{id}")]
        public HttpResponseMessage Get(Guid id)
        {
            try
            {
                var metric = _repository.GetByKey<Metric>(id);
                var model = Mapper.Map<MetricModel>(metric);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in GET /metrics/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion

        #region UPDATE
        [PUT("/v41/metrics/{id}")]
        public HttpResponseMessage Put(Guid id, [FromBody]ServerModel model)
        {
            try
            {
                var metric = _repository.GetByKey<Metric>(id);
                Mapper.Map(model, metric);

                _repository.Update(metric);
                _repository.UnitOfWork.SaveChanges();

                var returnModel = Mapper.Map<MetricModel>(metric);

                return Request.CreateResponse(HttpStatusCode.OK, returnModel);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in PUT /metrics/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }


        #endregion

        #region DELETE
        [DELETE("/v41/metrics/{id}")]
        public HttpResponseMessage Delete(Guid id)
        {
            try
            {
                var metric = _repository.GetByKey<Metric>(id);
                metric.Status = Status.Deleted;

                _repository.UnitOfWork.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogUnhandledException("Error in DELETE /metrics/" + id, ex);
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }
        #endregion
    }
}