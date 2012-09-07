CREATE TABLE [dbo].[Customers](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](1024) NOT NULL,
	[Status_Id] [int] NOT NULL DEFAULT 1,
	[Tenant_Id] [uniqueidentifier] NOT NULL,
	[ServiceDeskData] [nvarchar](max) NULL
	CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_Customer_Tenant] FOREIGN KEY([Tenant_Id]) REFERENCES [dbo].[Tenants] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_Customer_Status] FOREIGN KEY([Status_Id]) REFERENCES [dbo].[Statuses] ([Id]),
	CONSTRAINT [IDX_Name] UNIQUE ([Name])
)


