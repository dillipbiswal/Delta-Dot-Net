CREATE TABLE [dbo].[Schedules](
	[Id] [uniqueidentifier] NOT NULL,
	[Day] [int] NULL,
	[DayOfWeek] [int] NULL,
	[Hour] [int] NULL,
	[Month] [int] NULL,
	[Minute] [int] NULL,
	[ScheduleType_Id] [int] NOT NULL,
	[Interval] [int] NOT NULL,
	[MetricConfiguration_Id] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_Schedules] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_Schedule_MetricConfiguration] FOREIGN KEY([MetricConfiguration_Id]) REFERENCES [dbo].[MetricConfigurations] ([Id]) ON DELETE CASCADE
)


