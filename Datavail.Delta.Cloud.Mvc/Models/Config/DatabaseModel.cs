using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Domain;
using AutoMapper;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class DatabaseModel : IHaveCustomMappings
    {
        public Guid Id { get; set; }

        public Guid DatabaseInstanceId { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Name { get; set; }

        [Required]
        [UIHint("FilteredStatusDropDown")]
        public Status Status { get; set; }

        public string Oper { get; set; }

        public DatabaseModel()
        {
            Id = Guid.Empty;
            Status = Status.Active;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<Database, DatabaseModel>()
                .ForMember(f => f.Status, opt => opt.MapFrom(f => f.Status));

            configuration.CreateMap<DatabaseModel, Database>()
                .ForMember(m => m.Status, opt => opt.MapFrom(f => (Status)f.Status))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}