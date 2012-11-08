using System;

namespace Datavail.Delta.Domain
{
    public class Schedule : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public int? Day { get; set; }
        public virtual DayOfWeek DayOfWeek { get; set; }
        public virtual MetricConfiguration MetricConfiguration { get; set; }
        public int? Hour { get; set; }
        public int? Month { get; set; }
        public int? Minute { get; set; }
        public virtual ScheduleType ScheduleType { get; set; }
        public int Interval { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewSchedule factory instead")]
        public Schedule()
        {
            Initialize();
        }

#pragma warning disable 612,618
        /// <summary>
        /// Creates a schedule to run every X seconds, where X is intervalValue 
        /// </summary>
        /// <param name="metricconfiguration">The metric configuration item this schedule is associated with</param>
        /// <param name="intervalValue">The number of seconds that will be elapse before the schedule runs again</param>
        public static Schedule NewSecondsSchedule(MetricConfiguration metricconfiguration, int intervalValue)
        {
            var entity = new Schedule() { DayOfWeek = Domain.DayOfWeek.NotSpecified, MetricConfiguration = metricconfiguration, ScheduleType = Domain.ScheduleType.Seconds, Interval = intervalValue };
            return entity;
        }

        /// <summary>
        /// Creates a schedule to run every X minutes, where X is intervalValue 
        /// </summary>
        /// <param name="metricconfiguration">The metric configuration item this schedule is associated with</param>
        /// <param name="intervalValue">The number of minutes that will be elapse before the schedule runs again</param>
        public static Schedule NewMinutesSchedule(MetricConfiguration metricconfiguration, int intervalValue)
        {
            var entity = new Schedule() { DayOfWeek = Domain.DayOfWeek.NotSpecified, MetricConfiguration = metricconfiguration, ScheduleType = Domain.ScheduleType.Minutes, Interval = intervalValue };
            return entity;
        }

        /// <summary>
        /// Creates a schedule to run every X hours at Y minutes past the hour, where X is intervalValue and Y is minuteValue 
        /// </summary>
        /// <param name="metricconfiguration">The metric configuration item this schedule is associated with</param>
        /// <param name="intervalValue">The number of hours that will be elapse before the schedule runs again</param>
        /// <param name="minuteValue">The number of minutes past the hour the schedule will run on</param>
        public static Schedule NewHoursSchedule(MetricConfiguration metricconfiguration, int intervalValue, int minuteValue)
        {
            var entity = new Schedule() { DayOfWeek = Domain.DayOfWeek.NotSpecified, MetricConfiguration = metricconfiguration, ScheduleType = Domain.ScheduleType.Hours, Interval = intervalValue, Minute = minuteValue };
            return entity;
        }

        /// <summary>
        /// Creates a schedule to run every X days at Y hours and Z minutes, where X is intervalValue, Y is hourValue and Z is minuteValue 
        /// </summary>
        /// <param name="metricconfiguration">The metric configuration item this schedule is associated with</param>
        /// <param name="intervalValue">The number of days that will be elapse before the schedule runs again</param>
        /// <param name="hourValue">The hour of the day the schedule will run on</param>
        /// <param name="minuteValue">The number of minutes past the hour the schedule will run on</param>
        public static Schedule NewDaysSchedule(MetricConfiguration metricconfiguration, int intervalValue, int hourValue, int minuteValue)
        {
            var entity = new Schedule() { MetricConfiguration = metricconfiguration, ScheduleType = Domain.ScheduleType.Days, Hour = hourValue, Interval = intervalValue, Minute = minuteValue };
            return entity;
        }

        /// <summary>
        /// Creates a schedule to run every N weeks on the Xth day of the week at Y hours and Z minutes, where N is intervalValue, X is dayOfWeek, Y is hourValue and Z is minuteValue 
        /// </summary>
        /// <param name="metricconfiguration">The metric configuration item this schedule is associated with</param>
        /// <param name="intervalValue">The number of days that will be elapse before the schedule runs again</param>
        /// <param name="dayOfWeekValue">The day of the week the schedule will run on</param>
        /// <param name="hourValue">The hour of the day the schedule will run on</param>
        /// <param name="minuteValue">The number of minutes past the hour the schedule will run on</param>
        public static Schedule NewWeeksSchedule(MetricConfiguration metricconfiguration, int intervalValue, DayOfWeek dayOfWeekValue, int hourValue, int minuteValue)
        {
            var entity = new Schedule() { MetricConfiguration = metricconfiguration, ScheduleType = Domain.ScheduleType.Weeks, DayOfWeek = dayOfWeekValue, Hour = hourValue, Minute = minuteValue, Interval = intervalValue };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        #endregion
    }
}
