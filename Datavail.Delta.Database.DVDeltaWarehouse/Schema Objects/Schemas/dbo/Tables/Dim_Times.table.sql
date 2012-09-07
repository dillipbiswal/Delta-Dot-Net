CREATE TABLE [dbo].[Dim_Times](
	[TimeKey] [char](6)  NOT NULL,
	[Hour] [int] NULL,
	[Minute] [int] NULL,
	[Second] [int] NULL,
	[IsMorning] [bit] NULL,
	[IsAfternoon] [bit] NULL,
	[IsEvening] [bit] NULL,
	[AmPm] [char](2)  NULL,
	[HourMinute24Hour] [char](5)  NULL,
	[HourMinuteSecond24Hour] [char](8)  NULL,
	[HourMinute12Hour] [char](8)  NULL,
	[HourMinuteSecond12Hour] [char](11)  NULL,
	CONSTRAINT [PK_Dim_Times] PRIMARY KEY CLUSTERED ([TimeKey] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)


