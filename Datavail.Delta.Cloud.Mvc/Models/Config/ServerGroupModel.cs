using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class ServerGroupModel : IHaveCustomMappings
    {
        public Guid Id { get; set; }

        public Guid ParentId { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Name { get; set; }

        [Required]
        public int Priority { get; set; }

        [Required]
        [UIHint("FilteredStatusDropDown")]
        public Status Status { get; set; }

        public string Oper { get; set; }

        public ServerGroupModel()
        {
            Id = Guid.Empty;
            Priority = 0;
            Status = Status.Active;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<ServerGroup, ServerGroupModel>()
                .ForMember(f => f.Status, opt => opt.MapFrom(f => f.Status.Enum));

            configuration.CreateMap<ServerGroupModel, ServerGroup>()
                .ForMember(m => m.Status, opt => opt.MapFrom(f => (StatusWrapper)f.Status))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}
