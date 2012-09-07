using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using Datavail.Delta.Domain;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricConfigurationModel : IHaveCustomMappings
    {
        [HiddenInput(DisplayValue = false)]
        public Guid MetricId { get; set; }

        [HiddenInput(DisplayValue = false)]
        public Guid? Id { get; set; }

        public string Name { get; set; }
        public TableModel SchedulesTable { get; set; }
        public TableModel ThresholdsTable { get; set; }
        public bool IsTemplate { get; set; }

        public MetricConfigurationModel()
        {
            Id = Guid.Empty;
            SchedulesTable = new TableModel();
            ThresholdsTable = new TableModel();
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<MetricConfigurationModel, MetricConfiguration>()
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}