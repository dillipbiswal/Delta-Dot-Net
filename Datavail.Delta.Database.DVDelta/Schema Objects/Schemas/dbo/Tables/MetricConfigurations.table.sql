CREATE TABLE [dbo].[MetricConfigurations](
	[Id] [uniqueidentifier] NOT NULL,
	[IsTemplate] [bit] NOT NULL DEFAULT 0,
	[Name] [nvarchar](1024)  NOT NULL,
	[Metric_Id] [uniqueidentifier] NULL,
	[ParentCustomer_Id] [uniqueidentifier] NULL,
	[ParentMetricInstance_Id] [uniqueidentifier] NULL,
	[ParentServerGroup_Id] [uniqueidentifier] NULL,
	[ParentServer_Id] [uniqueidentifier] NULL,	
	[ParentTenant_Id] [uniqueidentifier] NULL,
	[Metric_Id1] [uniqueidentifier] NULL,
	[ParentMetric_Id] [uniqueidentifier] NULL,
	 CONSTRAINT [PK_MetricConfigurations] PRIMARY KEY CLUSTERED([Id] ASC)
	 WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	 CONSTRAINT [FK_MetricConfiguration_Customer] FOREIGN KEY([ParentCustomer_Id]) REFERENCES [dbo].[Customers] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_Metric] FOREIGN KEY([Metric_Id]) REFERENCES [dbo].[Metrics] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_Metric1] FOREIGN KEY([Metric_Id1]) REFERENCES [dbo].[Metrics] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_ParentMetric] FOREIGN KEY([ParentMetric_Id]) REFERENCES [dbo].[Metrics] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_MetricInstance] FOREIGN KEY([ParentMetricInstance_Id]) REFERENCES [dbo].[MetricInstances] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_Server] FOREIGN KEY([ParentServer_Id]) REFERENCES [dbo].[Servers] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_ServerGroup] FOREIGN KEY([ParentServerGroup_Id]) REFERENCES [dbo].[ServerGroups] ([Id]),
	 CONSTRAINT [FK_MetricConfiguration_Tenant] FOREIGN KEY([ParentTenant_Id]) REFERENCES [dbo].[Tenants] ([Id])
)
