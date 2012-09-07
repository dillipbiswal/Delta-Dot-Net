CREATE TABLE [dbo].[Servers](
	[Id] [uniqueidentifier] NOT NULL,
	[AgentVersion] [nvarchar](max)  NULL,
	[Hostname] [nvarchar](max)  NOT NULL,
	[IpAddress] [nvarchar](15)  NOT NULL,	
	[LastCheckIn] [datetime] NOT NULL,
	[Status_Id] [int] NOT NULL DEFAULT 1,
	[Customer_Id] [uniqueidentifier] NULL,
	[Tenant_Id] [uniqueidentifier] NOT NULL,
	[Cluster_Id] [uniqueidentifier] NULL,
	[IsVirtual] [bit] NOT NULL DEFAULT 0,
	[ClusterGroupName] [nvarchar](255) NULL,
	[VirtualServerParent_Id] [uniqueidentifier] NULL,
	[ActiveNode_Id] [uniqueidentifier] NULL,
	CONSTRAINT [PK_Servers] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_Server_Cluster] FOREIGN KEY([Cluster_Id]) REFERENCES [dbo].[Clusters] ([Id]),
	CONSTRAINT [FK_Server_Cluster_2] FOREIGN KEY([VirtualServerParent_Id]) REFERENCES [dbo].[Clusters] ([Id]),
	CONSTRAINT [FK_Server_Statuses] FOREIGN KEY([Status_Id]) REFERENCES [dbo].[Statuses] ([Id]),
	CONSTRAINT [FK_Server_Customer] FOREIGN KEY([Customer_Id]) REFERENCES [dbo].[Customers] ([Id]),
	CONSTRAINT [FK_Server_Tenant] FOREIGN KEY([Tenant_Id]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE CASCADE,
	--CONSTRAINT [FK_Server_Server] FOREIGN KEY([ActiveNode_Id]) REFERENCES [dbo].[Servers] ([Id])
)