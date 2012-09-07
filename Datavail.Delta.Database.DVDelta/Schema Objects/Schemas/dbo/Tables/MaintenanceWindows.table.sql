﻿CREATE TABLE [dbo].[MaintenanceWindows] (
	[Id]        UNIQUEIDENTIFIER NOT NULL,
	[BeginDate] DATETIME         NOT NULL,
	[EndDate]   DATETIME         NOT NULL,
	[Metric_Id] [uniqueidentifier] NULL,
	[Customer_Id] [uniqueidentifier] NULL,
	[ServerGroup_Id] [uniqueidentifier] NULL,
	[Server_Id] [uniqueidentifier] NULL,
	[MetricInstance_Id] [uniqueidentifier] NULL,
	[Tenant_Id] [uniqueidentifier] NULL,
	[ParentPreviousStatus_Value] [int] NULL,
	
	CONSTRAINT [PK_MaintenanceWindows] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_MaintenanceWindow_Customer] FOREIGN KEY([Customer_Id]) REFERENCES [dbo].[Customers] ([Id]),
	CONSTRAINT [FK_MaintenanceWindow_Metric] FOREIGN KEY([Metric_Id]) REFERENCES [dbo].[Metrics] ([Id]),
	CONSTRAINT [FK_MaintenanceWindow_MetricInstance] FOREIGN KEY([MetricInstance_Id]) REFERENCES [dbo].[MetricInstances] ([Id]),
	CONSTRAINT [FK_MaintenanceWindow_Server] FOREIGN KEY([Server_Id]) REFERENCES [dbo].[Servers] ([Id]),
	CONSTRAINT [FK_MaintenanceWindow_ServerGroup] FOREIGN KEY([ServerGroup_Id]) REFERENCES [dbo].[ServerGroups] ([Id]),
	CONSTRAINT [FK_MaintenanceWindow_Tenant] FOREIGN KEY([Tenant_Id]) REFERENCES [dbo].[Tenants] ([Id]),
	CONSTRAINT [FK_MaintenanceWindow_ParentPreviousStatus] FOREIGN KEY([ParentPreviousStatus_Value]) REFERENCES [dbo].[Statuses] ([Id]));