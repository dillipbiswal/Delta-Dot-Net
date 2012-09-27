using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Domain;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using System;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class ScheduleModel : IHaveCustomMappings
    {
        public Guid? Id { get; set; }

        public Guid MetricConfigId { get; set; }

        public int? Day { get; set; }

        [UIHint("FilteredDayDropDown")]
        public Datavail.Delta.Domain.DayOfWeek DayOfWeek { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }

        [UIHint("EnumDropDownList")]
        public ScheduleType ScheduleType { get; set; }
        public int? Interval { get; set; }
        public string Oper { get; set; }

        public string Time { 
            get { 
                return _Time; 
            } 
            set {
                _Time = value;
                Hour = Int32.Parse(_Time.Split(':')[0]);
                Minute = Int32.Parse(_Time.Split(':')[1]);
            } 
        }
        private string _Time;

        public ScheduleModel()
        {
            Id = Guid.Empty;
            DayOfWeek = Domain.DayOfWeek.NotSpecified;
            ScheduleType = Domain.ScheduleType.Once;
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<Schedule, ScheduleModel>()
                .ForMember(f => f.DayOfWeek, opt => opt.MapFrom(f => f.DayOfWeek))
                .ForMember(f => f.ScheduleType, opt => opt.MapFrom(f => f.ScheduleType));

            configuration.CreateMap<ScheduleModel, Schedule>()
                .ForMember(m => m.DayOfWeek, opt => opt.MapFrom(f => (Datavail.Delta.Domain.DayOfWeek)f.DayOfWeek))
                .ForMember(m => m.ScheduleType, opt => opt.MapFrom(f => (ScheduleType)f.ScheduleType))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}