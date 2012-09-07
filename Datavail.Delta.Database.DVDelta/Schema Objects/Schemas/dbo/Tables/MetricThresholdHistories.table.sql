CREATE TABLE [dbo].[MetricThresholdHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[MatchValue] [nvarchar](max)  NULL,
	[Percentage] [real] NOT NULL,
	[Timestamp] [datetime] NOT NULL,
	[Value] [bigint] NOT NULL,
	[MetricInstance_Id] [uniqueidentifier] NULL,
	[MetricThreshold_Id] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_MetricThresholdHistories] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_MetricThresholdHistory_MetricInstance] FOREIGN KEY([MetricInstance_Id]) REFERENCES [dbo].[MetricInstances] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_MetricThresholdHistory_MetricThreshold] FOREIGN KEY([MetricThreshold_Id]) REFERENCES [dbo].[MetricThresholds] ([Id]) ON DELETE CASCADE
)


