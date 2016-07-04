using System;
namespace Datavail.Delta.Domain
{
    public enum Status
    {
        Active = 1,
        Deleted = 9,
        Inactive = 0,
        InMaintenance = 2,
        Unknown = -1
    }

    public enum AgentError
    {
        Enabled = 1,
        Disabled = 0
    }

    //public class Status
    //{
    //    public Status Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (Status)value; }
    //    }

    //    public static implicit operator Status(Status w)
    //    {
    //        if (w == null) return default(Status);
    //        return w;
    //    }

    //    public static implicit operator Status(Status c)
    //    {
    //        return new Status { Enum = c };
    //    }

    //    public static implicit operator Status(int value)
    //    {
    //        return new Status { Value = value };
    //    }
    //}


    public enum Severity
    {
        Critical = 1,
        Severe = 2,
        Warning = 3,
        Informational = 4
    }

    //public class Severity
    //{
    //    public Severity Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (Severity)value; }
    //    }

    //    public static implicit operator Severity(Severity w)
    //    {
    //        return w == null ? default(Severity) : w;
    //    }

    //    public static implicit operator Severity(Severity c)
    //    {
    //        return new Severity { Enum = c };
    //    }
    //}

    public enum ThresholdValueType
    {
        Value,
        Percentage
    }

    //public class ThresholdValueType
    //{
    //    public ThresholdValueType Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (ThresholdValueType)value; }
    //    }

    //    public static implicit operator ThresholdValueType(ThresholdValueType w)
    //    {
    //        return w == null ? default(ThresholdValueType) : w;
    //    }

    //    public static implicit operator ThresholdValueType(ThresholdValueType c)
    //    {
    //        return new ThresholdValueType { Enum = c };
    //    }
    //}

    public enum ThresholdComparisonFunction
    {
        Average,
        Value,
        Match
    }

    //public class ThresholdComparisonFunction
    //{
    //    public ThresholdComparisonFunction Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (ThresholdComparisonFunction)value; }
    //    }

    //    public static implicit operator ThresholdComparisonFunction(ThresholdComparisonFunction w)
    //    {
    //        return w == null ? default(ThresholdComparisonFunction) : w;
    //    }

    //    public static implicit operator ThresholdComparisonFunction(ThresholdComparisonFunction c)
    //    {
    //        return new ThresholdComparisonFunction { Enum = c };
    //    }
    //}

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

    //public class ScheduleType
    //{
    //    public ScheduleType Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (ScheduleType)value; }
    //    }

    //    public static implicit operator ScheduleType(ScheduleType w)
    //    {
    //        return w == null ? default(ScheduleType) : w;
    //    }

    //    public static implicit operator ScheduleType(ScheduleType c)
    //    {
    //        return new ScheduleType { Enum = c };
    //    }
    //}

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

    //public class DayOfWeek
    //{
    //    public DayOfWeek Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (DayOfWeek)value; }
    //    }

    //    public static implicit operator DayOfWeek(DayOfWeek w)
    //    {
    //        return w == null ? default(DayOfWeek) : w;
    //    }

    //    public static implicit operator DayOfWeek(DayOfWeek c)
    //    {
    //        return new DayOfWeek { Enum = c };
    //    }
    //}

    [Flags]
    public enum MetricType
    {
        Server = 1,
        Instance = 2,
        Database = 4,
        VirtualServer = 8
    }

    //public class MetricType
    //{
    //    public MetricType Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (MetricType)value; }
    //    }

    //    public static implicit operator MetricType(MetricType w)
    //    {
    //        return w == null ? default(MetricType) : w;
    //    }

    //    public static implicit operator MetricType(MetricType c)
    //    {
    //        return new MetricType { Enum = c };
    //    }

    //    public static implicit operator MetricType(int value)
    //    {
    //        return new MetricType { Value = value };
    //    }
    //}

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

    //public class MetricThresholdType
    //{
    //    public MetricThresholdType Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (MetricThresholdType)value; }
    //    }

    //    public static implicit operator MetricThresholdType(MetricThresholdType w)
    //    {
    //        return w == null ? default(MetricThresholdType) : w;
    //    }

    //    public static implicit operator MetricThresholdType(MetricThresholdType c)
    //    {
    //        return new MetricThresholdType { Enum = c };
    //    }

    //    public static implicit operator MetricThresholdType(int value)
    //    {
    //        return new MetricThresholdType { Value = value };
    //    }
    //}

    public enum DatabaseVersion
    {
        None,
        SqlServer2000,
        SqlServer2005,
        SqlServer2008
    }

    //public class DatabaseVersion
    //{
    //    public DatabaseVersion Enum { get; set; }

    //    public int Value
    //    {
    //        get { return (int)Enum; }
    //        set { Enum = (DatabaseVersion)value; }
    //    }

    //    public static implicit operator DatabaseVersion(DatabaseVersion w)
    //    {
    //        return w == null ? default(DatabaseVersion) : w;
    //    }

    //    public static implicit operator DatabaseVersion(DatabaseVersion c)
    //    {
    //        return new DatabaseVersion { Enum = c };
    //    }

    //    public static implicit operator DatabaseVersion(int value)
    //    {
    //        return new DatabaseVersion { Value = value };
    //    }
    //}

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

    public enum ApiUriWindowParentType
    {
        Tenant = 0,
        Customer = 1,
        ServerGroup = 2,
        Server = 3,
        MetricInstance = 4,
        Metric = 5
    }
}
