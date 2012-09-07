CREATE TABLE [dbo].[Dim_Dates](
	[DateKey] [int] NOT NULL,
	[DayOfWeek] [char](10)  NULL,
	[WeekBeginDate] [smalldatetime] NULL,
	[WeekNumber] [int] NULL,
	[MonthNumber] [int] NULL,
	[MonthName] [nvarchar](20)  NULL,
	[MonthNameShort] [nvarchar](3)  NULL,
	[MonthEndDate] [smalldatetime] NULL,
	[DaysInMonth] [int] NULL,
	[YearMonth] [int] NULL,
	[QuarterNumber] [tinyint] NULL,
	[QuarterName] [char](6)  NULL,
	[Year] [int] NULL,
	[IsWeekend] [bit] NULL,
	[IsWorkday] [bit] NULL,
	[WeekendOrWeekday] [char](9)  NULL,
	[IsHoliday] [bit] NULL,
	[HolidayName] [nchar](25)  NULL,
 CONSTRAINT [PK_Date] PRIMARY KEY CLUSTERED 
(
	[DateKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)


