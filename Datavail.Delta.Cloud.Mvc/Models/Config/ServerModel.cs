using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using Datavail.Delta.Domain;
using System.ComponentModel.DataAnnotations;
using AutoMapper;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class ServerModel : IHaveCustomMappings
    {
        public Guid Id { get; set; }

        public Guid ClusterId { get; set; }

        [Required]
        public string Hostname { get; set; }

        public string IpAddress { get; set; }

        [Required]
        [UIHint("FilteredStatusDropDown")]
        public Status Status { get; set; }

        [Required]
        [UIHint("FilteredAgentErrorDropDown")]
        public AgentError AgentError { get; set; }

        public string Oper { get; set; }

        public string AgentVersion { get; set; }

        public DateTime? LastCheckIn { get; set; }

        public string ClusterGroupName { get; set; }

        public bool IsVirtual { get; set; }

        public ServerModel()
        {
            Id = Guid.Empty;
            Status = Status.Active;
            IsVirtual = false;
            IpAddress = string.Empty;
            AgentError = AgentError.Enabled;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<Server, ServerModel>()
                .ForMember(f => f.Status, opt => opt.MapFrom(f => f.Status))
                .ForMember(x => x.AgentError, opt => opt.MapFrom(x => x.AgentError));

            configuration.CreateMap<ServerModel, Server>()
                .ForMember(m => m.Status, opt => opt.MapFrom(f => (Status)f.Status))
                .ForMember(y => y.AgentError, opt => opt.MapFrom(y => (AgentError)y.AgentError))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}