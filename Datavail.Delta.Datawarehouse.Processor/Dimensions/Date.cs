using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using Datavail.Delta.Infrastructure.Extensions;

namespace Datavail.Delta.Datawarehouse.Processor.Dimensions
{
    public class Date
    {
        [Key]
        public int DateKey { get; set; }

        [MaxLength(10)]
        public string DayOfWeek { get; set; }

        public DateTime WeekBeginDate { get; set; }
        public int WeekNumber { get; set; }
        public int MonthNumber { get; set; }

        [MaxLength(10)]
        public string MonthName { get; set; }
        [MaxLength(3)]
        public string MonthNameShort { get; set; }

        public DateTime MonthEndDate { get; set; }
        public int DaysInMonth { get; set; }

        [MaxLength(10)]
        public string YearMonth { get; set; }

        public int QuarterNumber { get; set; }
        [MaxLength(2)]
        public string QuarterName { get; set; }

        public int Year { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsWorkday { get; set; }
        public bool IsHoliday { get; set; }
        [MaxLength(255)]
        public string HolidayName { get; set; }

        public static Date GetFromDateTime(DateTime inputDate)
        {
            var date = new Date
                           {
                               DateKey = Int32.Parse(string.Format("{0}{1}{2}", inputDate.Year,
                                                                   inputDate.Month.ToString(CultureInfo.InvariantCulture)
                                                                       .PadLeft(2, '0'),
                                                                   inputDate.Day.ToString(CultureInfo.InvariantCulture).
                                                                       PadLeft(2, '0'))),
                               DayOfWeek = inputDate.DayOfWeek.ToString(),
                               WeekBeginDate = inputDate.StartOfWeek(System.DayOfWeek.Sunday),
                               WeekNumber = GetWeekNumber(inputDate),
                               MonthNumber = inputDate.Month,
                               MonthName = GetMonthName(inputDate.Month),
                               MonthNameShort = GetMonthNameShort(inputDate.Month),
                               MonthEndDate = new DateTime(inputDate.Year, inputDate.Month + 1, 1).AddDays(-1),
                               DaysInMonth = DateTime.DaysInMonth(inputDate.Year, inputDate.Month),
                               YearMonth = string.Format("{0}{1}", inputDate.Year, inputDate.Month.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')),
                               QuarterNumber = GetQuarterNumber(inputDate.Month),
                               QuarterName = "Q" + GetQuarterNumber(inputDate.Month),
                               Year = inputDate.Year,
                               IsWeekend = inputDate.DayOfWeek == System.DayOfWeek.Saturday || inputDate.DayOfWeek == System.DayOfWeek.Sunday,
                               IsWorkday = inputDate.DayOfWeek != System.DayOfWeek.Saturday && inputDate.DayOfWeek != System.DayOfWeek.Sunday,
                               IsHoliday = false
                           };
            return date;
        }

        private static int GetWeekNumber(DateTime inputDate)
        {
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;
            return cal.GetWeekOfYear(inputDate, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }

        private static string GetMonthName(int monthNumber)
        {
            switch (monthNumber)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
            }

            throw new InvalidOperationException("Invalid Month Number Specified");
        }

        private static string GetMonthNameShort(int monthNumber)
        {
            switch (monthNumber)
            {
                case 1:
                    return "Jan";
                case 2:
                    return "Feb";
                case 3:
                    return "Mar";
                case 4:
                    return "Apr";
                case 5:
                    return "May";
                case 6:
                    return "Jun";
                case 7:
                    return "Jul";
                case 8:
                    return "Aug";
                case 9:
                    return "Sep";
                case 10:
                    return "Oct";
                case 11:
                    return "Nov";
                case 12:
                    return "Dec";
            }

            throw new InvalidOperationException("Invalid Month Number Specified");
        }

        private static int GetQuarterNumber(int monthNumber)
        {
            if (monthNumber == 1 || monthNumber == 2 || monthNumber == 3)
                return 1;

            if (monthNumber == 4 || monthNumber == 5 || monthNumber == 6)
                return 2;

            if (monthNumber == 7 || monthNumber == 8 || monthNumber == 9)
                return 3;

            if (monthNumber == 10 || monthNumber == 11 || monthNumber == 12)
                return 4;

            throw new InvalidOperationException("Invalid Month Number Specified");
        }
    }
}
