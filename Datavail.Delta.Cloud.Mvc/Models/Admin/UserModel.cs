using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Admin
{
    public class UserModel : IHaveCustomMappings
    {
        public Guid? Id { get; set; }

        [Required]
        public string EmailAddress { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string Password { get; set; }

        public string Oper { get; set; }

        public AddRoleModel RoleModel { get; set; }

        public UserModel()
        {
            Id = Guid.Empty;
            RoleModel = new AddRoleModel();
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<UserModel, User>()
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}