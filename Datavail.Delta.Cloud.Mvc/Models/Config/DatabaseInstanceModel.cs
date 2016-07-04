using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Datavail.Delta.Domain;
using Datavail.Delta.Cloud.Mvc.Infrastructure;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class DatabaseInstanceModel : IHaveCustomMappings
    {
        public Guid Id { get; set; }

        public Guid ServerId { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Name { get; set; }

        [MaxLength(1024)]
        public string Username { get; set; }

        [MaxLength(1024)]
        public string Password { get; set; }

        [Required]
        public bool UseIntegratedSecurity { get; set; }

        [Required]
        [UIHint("FilteredStatusDropDown")]
        public Status Status { get; set; }

        [Required]
        [UIHint("FilteredDatabaseVersionDropDown")]
        public DatabaseVersion DatabaseVersion { get; set; }

        public string Oper { get; set; }

        public Constants.TableType Parent { get; set; }

        public DatabaseInstanceModel()
        {
            Id = Guid.Empty;
            Status = Status.Active;
            DatabaseVersion = Domain.DatabaseVersion.None;
            UseIntegratedSecurity = true;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<DatabaseInstance, DatabaseInstanceModel>()
                .ForMember(f => f.Status, opt => opt.MapFrom(f => f.Status))
                .ForMember(f => f.DatabaseVersion, opt => opt.MapFrom(f => f.DatabaseVersion));

            configuration.CreateMap<DatabaseInstanceModel, DatabaseInstance>()
                .ForMember(m => m.Status, opt => opt.MapFrom(f => (Status)f.Status))
                .ForMember(m => m.DatabaseVersion, opt => opt.MapFrom(f => (DatabaseVersion)f.DatabaseVersion))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}