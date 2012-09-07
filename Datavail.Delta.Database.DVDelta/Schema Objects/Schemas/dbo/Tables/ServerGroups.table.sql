CREATE TABLE [dbo].[ServerGroups](
	[Id] [uniqueidentifier] NOT NULL,	
	[Name] [nvarchar](1024)  NOT NULL,
	[Priority] [int] NOT NULL,
	[Status_Id] [int] NOT NULL DEFAULT 1,
	[ParentTenant_Id] [uniqueidentifier] NULL,
	[ParentCustomer_Id] [uniqueidentifier] NULL,
	CONSTRAINT [PK_ServerGroups] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_ServerGroup_Statuses] FOREIGN KEY([Status_Id]) REFERENCES [dbo].[Statuses] ([Id]),
	CONSTRAINT [FK_ServerGroup_Customer] FOREIGN KEY([ParentCustomer_Id]) REFERENCES [dbo].[Customers] ([Id]),
	CONSTRAINT [FK_ServerGroup_Tenant] FOREIGN KEY([ParentTenant_Id]) REFERENCES [dbo].[Tenants] ([Id])
)
