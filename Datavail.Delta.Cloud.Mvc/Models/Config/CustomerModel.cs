using System;
using System.ComponentModel.DataAnnotations;
using Datavail.Delta.Domain;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Validation;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class CustomerModel : IHaveCustomMappings
    {
        public Guid? Id { get; set; }

        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Name { get; set; }

        [Required]
        [UIHint("FilteredStatusDropDown")]
        public Status Status { get; set; }

        public string Oper { get; set; }

        public CustomerModel()
        {
            Id = Guid.Empty;
            Status = Status.Active;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<Customer, CustomerModel>()
                .ForMember(f => f.Status, opt => opt.MapFrom(f => f.Status.Enum));
            
            configuration.CreateMap<CustomerModel, Customer>()
                .ForMember(m => m.Status, opt => opt.MapFrom(f => (StatusWrapper)f.Status))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}