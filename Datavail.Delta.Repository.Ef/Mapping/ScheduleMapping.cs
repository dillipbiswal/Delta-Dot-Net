using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class ScheduleMapping : EntityTypeConfiguration<Schedule>
    {
        public ScheduleMapping()
        {
            HasKey(x => x.Id);
            Property(x => x.Day).IsOptional();
            Property(x => x.DayOfWeek.Value).IsOptional().HasColumnName("DayOfWeek");
            Property(x => x.Hour).IsOptional();
            HasRequired(x => x.MetricConfiguration).WithMany(x => x.Schedules).WillCascadeOnDelete(true);
            Property(x => x.Minute).IsOptional();
            Property(x => x.Month).IsOptional();
            Property(x => x.ScheduleType.Value).HasColumnName("ScheduleType_Id");
            ToTable("Schedules");
        }
    }
}