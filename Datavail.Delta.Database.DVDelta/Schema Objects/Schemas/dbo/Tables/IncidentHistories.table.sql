CREATE TABLE [dbo].[IncidentHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[CloseTimestamp] [datetime] NULL,
	[IncidentNumber] [nvarchar](255)  NULL,
	[OpenTimestamp] [datetime] NOT NULL,
	[MetricInstance_Id] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_IncidentHistories] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_IncidentHistory_MetricInstance] FOREIGN KEY([MetricInstance_Id]) REFERENCES [dbo].[MetricInstances] ([Id]) ON DELETE CASCADE
)


