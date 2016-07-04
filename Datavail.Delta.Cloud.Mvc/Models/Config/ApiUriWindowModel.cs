using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using Datavail.Delta.Domain;
using System.ComponentModel.DataAnnotations;
using System.Configuration;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class ApiUriWindowModel : IHaveCustomMappings
    {
        public Guid? Id { get; set; }
        public Guid ParentId { get; set; }

        public string PlugInName { get; set; }
        public string URIAddress { get; set; }
        public string AgentServerId { get; set; }
        public string HostName { get; set; }
        public ApiUriWindowParentType ParentType { get; set; }

        public Guid CustomerId { get; set; }

        public IEnumerable<Server> HostNamelist { get; set; }
        public IEnumerable<Metric> Pluginlist { get; set; }
        public bool FlagApplyToAll { get; set; }
        public ApiUriWindowModel()
        {
            Id = Guid.Empty;
            PlugInName = String.Empty;
            ParentId = Guid.Empty;
            URIAddress = ConfigurationManager.AppSettings["DefApiUrl"];
            AgentServerId = String.Empty;
            CustomerId = Guid.Empty;
            HostName = string.Empty;
            FlagApplyToAll = false;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<ApiUriWindowModel, ApiUri>()
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}