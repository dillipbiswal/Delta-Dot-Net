CREATE TABLE [dbo].[Tenants](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](1024) NOT NULL,
	[Status_Id] [int] NOT NULL DEFAULT 1,
	CONSTRAINT [PK_Tenants] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_Tenant_Status] FOREIGN KEY([Status_Id]) REFERENCES [dbo].[Statuses] ([Id]),
	CONSTRAINT [IDX_Tenant_Name] UNIQUE ([Name])
)


