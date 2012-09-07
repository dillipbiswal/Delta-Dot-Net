CREATE TABLE [dbo].[Clusters](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](1024)  NULL,
	[Customer_Id] [uniqueidentifier] NOT NULL DEFAULT 'E44F16FA-A954-455F-871B-49B9E3206991',
	[Status_Id] [int] NOT NULL DEFAULT 1,
	CONSTRAINT [PK_Clusters] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_Cluster_Customer] FOREIGN KEY([Customer_Id]) REFERENCES [dbo].[Customers] ([Id])
)
