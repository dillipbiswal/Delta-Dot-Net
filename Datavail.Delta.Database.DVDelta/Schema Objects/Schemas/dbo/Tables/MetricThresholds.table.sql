CREATE TABLE [dbo].[MetricThresholds](
	[Id] [uniqueidentifier] NOT NULL,
	[CeilingValue] [real] NOT NULL,
	[FloorValue] [real] NOT NULL,
	[MatchValue] [nvarchar](max)  NULL,
	[NumberOfOccurrences] [int] NOT NULL,
	[Severity_Value] [int] NOT NULL,
	[ThresholdComparisonFunction_Value] [int] NOT NULL,
	[ThresholdValueType_Value] [int] NOT NULL,
	[TimePeriod] [int] NOT NULL,
	[MetricConfiguration_Id] [uniqueidentifier] NULL,
	CONSTRAINT [PK_MetricThresholds] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_MetricThrehsold_MetricConfiguration] FOREIGN KEY([MetricConfiguration_Id]) REFERENCES [dbo].[MetricConfigurations] ([Id])
)


