using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using Datavail.Delta.Domain;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MaintenanceWindowModel : IHaveCustomMappings
    {
        public Guid? Id { get; set; }

        public Guid ParentId { get; set; }

        [Required]
        public DateTime BeginDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public MaintenanceWindowParentType ParentType { get; set; }

        public string Oper { get; set; }

        public MaintenanceWindowModel()
        {
            //BeginDate = DateTime.Parse(DateTime.Now.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt"));
            //EndDate = DateTime.Parse(DateTime.Now.ToLocalTime().AddHours(1).ToString("MM/dd/yyyy hh:mm tt"));

            BeginDate = DateTime.Now;
            EndDate = DateTime.Now.AddHours(1);

            Id = Guid.Empty;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<MaintenanceWindowModel, MaintenanceWindow>()
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}