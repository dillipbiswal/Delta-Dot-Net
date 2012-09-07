using System;
namespace Datavail.Delta.Domain
{
    public enum Status
    {
        Active=1,
        Deleted = 9,
        Inactive=0,
        InMaintenance=2,
        Unknown=-1
    }

    public class StatusWrapper
    {
        public Status Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (Status)value; }
        }

        public static implicit operator Status(StatusWrapper w)
        {
            if (w == null) return default(Status);
            return w.Enum;
        }

        public static implicit operator StatusWrapper(Status c)
        {
            return new StatusWrapper { Enum = c };
        }

        public static implicit operator StatusWrapper(int value)
        {
            return new StatusWrapper { Value = value };
        }
    }

   
    public enum Severity
    {
        Critical = 1,
        Severe = 2,
        Warning = 3,
        Informational = 4
    }

    public class SeverityWrapper
    {
        public Severity Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (Severity)value; }
        }

        public static implicit operator Severity(SeverityWrapper w)
        {
            return w == null ? default(Severity) : w.Enum;
        }

        public static implicit operator SeverityWrapper(Severity c)
        {
            return new SeverityWrapper { Enum = c };
        }
    }

    public enum ThresholdValueType
    {
        Value,
        Percentage
    }

    public class ThresholdValueTypeWrapper
    {
        public ThresholdValueType Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (ThresholdValueType)value; }
        }

        public static implicit operator ThresholdValueType(ThresholdValueTypeWrapper w)
        {
            return w == null ? default(ThresholdValueType) : w.Enum;
        }

        public static implicit operator ThresholdValueTypeWrapper(ThresholdValueType c)
        {
            return new ThresholdValueTypeWrapper { Enum = c };
        }
    }

    public enum ThresholdComparisonFunction
    {
        Average,
        Value,
        Match
    }

    public class ThresholdComparisonFunctionWrapper
    {
        public ThresholdComparisonFunction Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (ThresholdComparisonFunction)value; }
        }

        public static implicit operator ThresholdComparisonFunction(ThresholdComparisonFunctionWrapper w)
        {
            return w == null ? default(ThresholdComparisonFunction) : w.Enum;
        }

        public static implicit operator ThresholdComparisonFunctionWrapper(ThresholdComparisonFunction c)
        {
            return new ThresholdComparisonFunctionWrapper { Enum = c };
        }
    }

    public enum ScheduleType
    {
        Once = -1,
        Seconds = 0,
        Minutes,
        Hours,
        Days,
        Weeks,
        Months,
        Year
    }

    public class ScheduleTypeWrapper
    {
        public ScheduleType Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (ScheduleType)value; }
        }

        public static implicit operator ScheduleType(ScheduleTypeWrapper w)
        {
            return w == null ? default(ScheduleType) : w.Enum;
        }

        public static implicit operator ScheduleTypeWrapper(ScheduleType c)
        {
            return new ScheduleTypeWrapper { Enum = c };
        }
    }

    public enum DayOfWeek
    {
        NotSpecified = -1,
        Sunday = 0,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    public class DayOfWeekWrapper
    {
        public DayOfWeek Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (DayOfWeek)value; }
        }

        public static implicit operator DayOfWeek(DayOfWeekWrapper w)
        {
            return w == null ? default(DayOfWeek) : w.Enum;
        }

        public static implicit operator DayOfWeekWrapper(DayOfWeek c)
        {
            return new DayOfWeekWrapper { Enum = c };
        }
    }

    [Flags]
    public enum MetricType
    {
        Server = 1,
        Instance = 2,
        Database = 4,
        VirtualServer = 8
    }

    public class MetricTypeWrapper
    {
        public MetricType Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (MetricType)value; }
        }

        public static implicit operator MetricType(MetricTypeWrapper w)
        {
            return w == null ? default(MetricType) : w.Enum;
        }

        public static implicit operator MetricTypeWrapper(MetricType c)
        {
            return new MetricTypeWrapper { Enum = c };
        }

        public static implicit operator MetricTypeWrapper(int value)
        {
            return new MetricTypeWrapper { Value = value };
        }
    }

    [Flags]
    public enum MetricThresholdType
    {
        None = 0,
        ValueComparison = 1,
        AverageComparison = 2,
        MatchComparison = 4,
        ValueType = 8,
        PercentageType = 16
    }

    public class MetricThresholdTypeWrapper
    {
        public MetricThresholdType Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (MetricThresholdType)value; }
        }

        public static implicit operator MetricThresholdType(MetricThresholdTypeWrapper w)
        {
            return w == null ? default(MetricThresholdType) : w.Enum;
        }

        public static implicit operator MetricThresholdTypeWrapper(MetricThresholdType c)
        {
            return new MetricThresholdTypeWrapper { Enum = c };
        }

        public static implicit operator MetricThresholdTypeWrapper(int value)
        {
            return new MetricThresholdTypeWrapper { Value = value };
        }
    }

    public enum DatabaseVersion
    {
        None,
        SqlServer2000,
        SqlServer2005,
        SqlServer2008
    }

    public class DatabaseVersionWrapper
    {
        public DatabaseVersion Enum { get; set; }

        public int Value
        {
            get { return (int)Enum; }
            set { Enum = (DatabaseVersion)value; }
        }

        public static implicit operator DatabaseVersion(DatabaseVersionWrapper w)
        {
            return w == null ? default(DatabaseVersion) : w.Enum;
        }

        public static implicit operator DatabaseVersionWrapper(DatabaseVersion c)
        {
            return new DatabaseVersionWrapper { Enum = c };
        }

        public static implicit operator DatabaseVersionWrapper(int value)
        {
            return new DatabaseVersionWrapper { Value = value };
        }
    }

    public enum MetricConfigurationParentType
    {
        Tenant = 0,
        Customer = 1,
        ServerGroup = 2,
        Server = 3,
        MetricInstance = 4,
        Metric = 5
    }

    public enum MaintenanceWindowParentType
    {
        Tenant = 0,
        Customer = 1,
        ServerGroup = 2,
        Server = 3,
        MetricInstance = 4,
        Metric = 5
    }

    public enum MetricInstanceParentType
    {
        Server = 0,
        Instance = 1,
        Database = 2,
        VirtualServer = 3
    }
}
