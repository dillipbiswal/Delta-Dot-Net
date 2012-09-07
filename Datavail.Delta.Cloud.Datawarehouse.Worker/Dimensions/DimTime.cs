using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimTime
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string TimeKey { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }
        public bool IsMorning { get; set; }
        public bool IsAfternoon { get; set; }
        public bool IsEvening { get; set; }
        public string AmPm { get; set; }
        public string HourMinute24Hour { get; set; }
        public string HourMinuteSecond24Hour { get; set; }
        public string HourMinute12Hour { get; set; }
        public string HourMinuteSecond12Hour { get; set; }

        public static DimTime CreateTimeDimensionEntry(int hour, int minute, int second)
        {
            var hour12 = hour;
            if (hour > 12)
            {
                hour12 = hour - 12;
            }
            if (hour == 0)
            {
                hour12 = 12;
            }

            var dimTime = new DimTime
                           {
                               TimeKey = hour.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'),
                               Hour = hour,
                               Minute = minute,
                               Second = second,
                               IsMorning = hour <= 11,
                               IsAfternoon = hour > 11 && hour < 18,
                               IsEvening = hour >= 18,
                               AmPm = hour < 12 ? "AM" : "PM",
                               HourMinute24Hour = hour.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'),
                               HourMinuteSecond24Hour = hour.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'),
                               HourMinute12Hour = hour < 12 ? hour12.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + " AM" : hour12.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + " PM",
                               HourMinuteSecond12Hour = hour < 12 ? hour12.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + " AM" : hour12.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + ":" + second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + " PM"
                           };

            return dimTime;
        }


        public static string GetSurrogateKeyFromTimestamp(DateTime timestamp) { return timestamp.Hour.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + timestamp.Minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + timestamp.Second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'); }
    }
}