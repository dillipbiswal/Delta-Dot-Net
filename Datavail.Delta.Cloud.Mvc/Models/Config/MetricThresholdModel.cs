using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping;
using AutoMapper;
using Datavail.Delta.Domain;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class MetricThresholdModel : IHaveCustomMappings
    {
        public Guid? Id { get; set; }

        public Guid MetricConfigId { get; set; }

        public float? CeilingValue { get; set; }
        public float? FloorValue { get; set; }
        public string MatchValue { get; set; }
        public int? NumberOfOccurrences { get; set; }

        public string Oper { get; set; }

        [UIHint("EnumDropDownList")]
        [Required]
        public Severity Severity { get; set; }

        public ThresholdComparisonFunction ThresholdComparisonFunction { get; set; }
        public ThresholdValueType ThresholdValueType { get; set; }
        
        public MetricThresholdType MetricThresholdType { get; set; }

        [Required]
        public int? TimePeriod { get; set; }

        public MetricThresholdModel(MetricThresholdType metricThresholdType)
        {
            Id = Guid.Empty;
            Severity = Domain.Severity.Critical;
            SetThresholdComparisonType(metricThresholdType);
            ThresholdValueType = ThresholdValueType.Value;
            MetricThresholdType = metricThresholdType;
        }

        public MetricThresholdModel()
        {
            Id = Guid.Empty;
            Severity = Domain.Severity.Critical;
            ThresholdComparisonFunction = ThresholdComparisonFunction.Average;
            ThresholdValueType = ThresholdValueType.Value;
        }

        public void SetThresholdComparisonType(MetricThresholdType metricThresholdtype)
        {
            if (((int)metricThresholdtype & (int)MetricThresholdType.AverageComparison) == (int)MetricThresholdType.AverageComparison)
            {
                ThresholdComparisonFunction = ThresholdComparisonFunction.Average;
                return;
            }

            if (((int)metricThresholdtype & (int)MetricThresholdType.ValueComparison) == (int)MetricThresholdType.ValueComparison)
            {
                ThresholdComparisonFunction = ThresholdComparisonFunction.Value;
                return;
            }

            if (((int)metricThresholdtype & (int)MetricThresholdType.MatchComparison) == (int)MetricThresholdType.MatchComparison)
            {
                ThresholdComparisonFunction = ThresholdComparisonFunction.Match;
                return;
            }
        }

        private SelectList GetThresholdValueSelectList(MetricThresholdType metricThresholdtype, ThresholdValueType thresholdsValueType = Domain.ThresholdValueType.Value)
        {
            var selectListItems = new List<SelectListItem>();
            var selected = false;

            if (((int)metricThresholdtype & (int)MetricThresholdType.ValueType) == (int)MetricThresholdType.AverageComparison)
            {
                if (thresholdsValueType == Domain.ThresholdValueType.Value)
                {
                    selected = true;
                }
                selectListItems.Add(new SelectListItem
                {
                    Text = Domain.ThresholdValueType.Value.ToString(),
                    Value = ((int)Domain.ThresholdValueType.Value).ToString(),
                    Selected = selected
                });
            }

            if (((int)metricThresholdtype & (int)MetricThresholdType.ValueType) == (int)MetricThresholdType.AverageComparison)
            {
                if (thresholdsValueType == Domain.ThresholdValueType.Value)
                {
                    selected = true;
                }
                selectListItems.Add(new SelectListItem
                {
                    Text = Domain.ThresholdValueType.Value.ToString(),
                    Value = ((int)Domain.ThresholdValueType.Value).ToString(),
                    Selected = selected
                });
            }

            return new SelectList(selectListItems);
        }

        void IHaveCustomMappings.CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<MetricThreshold, MetricThresholdModel>()
                .ForMember(f => f.Severity, opt => opt.MapFrom(f => f.Severity))
                .ForMember(f => f.ThresholdComparisonFunction, opt => opt.MapFrom(f => f.ThresholdComparisonFunction))
                .ForMember(f => f.ThresholdValueType, opt => opt.MapFrom(f => f.ThresholdValueType));

            configuration.CreateMap<MetricThresholdModel, MetricThreshold>()
                .ForMember(m => m.Severity, opt => opt.MapFrom(f => (Severity)f.Severity))
                .ForMember(m => m.ThresholdComparisonFunction, opt => opt.MapFrom(f => (ThresholdComparisonFunction)f.ThresholdComparisonFunction))
                .ForMember(m => m.ThresholdValueType, opt => opt.MapFrom(f => (ThresholdValueType)f.ThresholdValueType))
                .ForAllMembers(opt => opt.Condition(f => f.SourceValue != null));
        }
    }
}