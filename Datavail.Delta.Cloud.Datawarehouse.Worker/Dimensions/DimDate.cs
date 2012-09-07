using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions
{
    public class DimDate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DateKey { get; set; }
        public string DayOfWeek { get; set; }
        public DateTime WeekBeginDate { get; set; }
        public int WeekNumber { get; set; }
        public int MonthNumber { get; set; }
        public string MonthName { get; set; }
        public string MonthNameShort { get; set; }
        public DateTime MonthEndDate { get; set; }
        public int DaysInMonth { get; set; }
        public int YearMonth { get; set; }
        public int QuarterNumber { get; set; }
        public string QuarterName { get; set; }
        public int Year { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsWorkday { get; set; }
        public string WeekendOrWeekday { get; set; }
        public bool IsHoliday { get; set; }
        public string HolidayName { get; set; }

        public static DimDate CreateDateDimensionEntry(DateTime dateTime)
        {
            var dimDate = new DimDate
             {
                 DateKey = GetSurrogateKeyFromTimestamp(dateTime),
                 DayOfWeek = GetDayOfWeek(dateTime),
                 WeekBeginDate = GetWeekBeginDate(dateTime),
                 WeekNumber = (short)GetWeekNumber(dateTime),
                 MonthNumber = (short)dateTime.Month,
                 MonthName = GetMonthName(dateTime),
                 MonthNameShort = GetMonthNameShort(dateTime),
                 MonthEndDate = GetMonthEndDate(dateTime),
                 DaysInMonth = (short)GetDaysInMonth(dateTime),
                 YearMonth = GetYearMonth(dateTime),
                 QuarterNumber = (byte)GetQuarterNumber(dateTime),
                 QuarterName = GetQuarterName(dateTime),
                 Year = (short)dateTime.Year,
                 IsWeekend = GetIsWeekend(dateTime),
                 IsWorkday = GetIsWorkDay(dateTime),
                 WeekendOrWeekday = GetWeekendOrWorkday(dateTime)
             };

            return dimDate;
        }

        public static int GetSurrogateKeyFromTimestamp(DateTime dateTime)
        {
            var dateKey = string.Format("{0}{1}{2}", dateTime.Year, dateTime.Month.ToString().PadLeft(2, '0'), dateTime.Day.ToString().PadLeft(2, '0'));
            return Int32.Parse(dateKey);
        }

        public static string GetDayOfWeek(DateTime dateTime)
        {
            return dateTime.DayOfWeek.ToString();
        }

        public static DateTime GetWeekBeginDate(DateTime dateTime)
        {
            return dateTime.AddDays(-1 * (int)dateTime.DayOfWeek);
        }

        public static int GetWeekNumber(DateTime dateTime)
        {
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;

            return cal.GetWeekOfYear(dateTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }

        public static int GetMonthNumber(DateTime dateTime)
        {
            return dateTime.Month;
        }

        public static string GetMonthName(DateTime dateTime)
        {
            return dateTime.ToString("MMMM");
        }

        public static string GetMonthNameShort(DateTime dateTime)
        {
            return dateTime.ToString("MMM");
        }

        public static DateTime GetMonthEndDate(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.AddMonths(1).Month, 1).AddDays(-1);
        }

        public static int GetDaysInMonth(DateTime dateTime)
        {
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;

            return cal.GetDaysInMonth(dateTime.Year, dateTime.Month);
        }

        public static int GetYearMonth(DateTime dateTime)
        {
            return Int32.Parse(string.Format("{0}{1}", dateTime.Year, dateTime.Month.ToString().PadLeft(2, '0')));
        }

        public static int GetQuarterNumber(DateTime dateTime)
        {
            if (dateTime.Month <= 3)
                return 1;
            if (dateTime.Month >= 4 && dateTime.Month <= 6)
                return 2;
            if (dateTime.Month >= 7 && dateTime.Month <= 9)
                return 3;

            return 4;
        }

        public static string GetQuarterName(DateTime dateTime)
        {
            return "Q" + GetQuarterNumber(dateTime) + dateTime.Year;
        }

        public static bool GetIsWeekend(DateTime dateTime)
        {
            var dayOfWeek = (int)dateTime.DayOfWeek;
            return dayOfWeek == 0 || dayOfWeek == 6;
        }

        public static bool GetIsWorkDay(DateTime dateTime)
        {
            var dayOfWeek = (int)dateTime.DayOfWeek;
            return dayOfWeek != 0 && dayOfWeek != 6;
        }

        public static string GetWeekendOrWorkday(DateTime dateTime)
        {
            return GetIsWorkDay(dateTime) ? "Work Day" : "Weekend";
        }
    }
}
