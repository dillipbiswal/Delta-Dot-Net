CREATE TABLE [dbo].[MetricInstances](
	[Id] [uniqueidentifier] NOT NULL,
	[Data] [nvarchar](max)  NULL,
	[Label] [nvarchar](1024)  NULL,
	[Metric_Id] [uniqueidentifier] NOT NULL,
	[Server_Id] [uniqueidentifier] NOT NULL,
	[DatabaseInstance_Id] [uniqueidentifier] NULL,
	[Database_Id] [uniqueidentifier] NULL,
	[Status_Id] [int] NOT NULL DEFAULT 1,
	CONSTRAINT [PK_MetricInstances] PRIMARY KEY CLUSTERED([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_MetricInstance_Metric] FOREIGN KEY([Metric_Id]) REFERENCES [dbo].[Metrics] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_MetricInstance_Server] FOREIGN KEY([Server_Id]) REFERENCES [dbo].[Servers] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_MetricInstance_DatabaseInstance] FOREIGN KEY([DatabaseInstance_Id]) REFERENCES [dbo].[DatabaseInstances] ([Id]) ON DELETE NO ACTION,
	CONSTRAINT [FK_MetricInstance_Database] FOREIGN KEY([Database_Id]) REFERENCES [dbo].[Databases] ([Id]) ON DELETE NO ACTION)
