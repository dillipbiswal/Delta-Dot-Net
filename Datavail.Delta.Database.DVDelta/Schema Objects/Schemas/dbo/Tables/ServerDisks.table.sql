CREATE TABLE [dbo].[ServerDisks](
	[Id] [uniqueidentifier] NOT NULL,
	[Label] [nvarchar](1024)  NULL,
	[Path] [nvarchar](2048)  NULL,
	[Cluster_Id] [uniqueidentifier] NULL,
	[Server_Id] [uniqueidentifier] NULL,
	[TotalBytes] [bigint] NULL,
	CONSTRAINT [PK_ServerDisks] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_ServerDisk_Server] FOREIGN KEY([Server_Id]) REFERENCES [dbo].[Servers] ([Id]),
	CONSTRAINT [FK_ServerDisk_Cluster] FOREIGN KEY([Cluster_Id]) REFERENCES [dbo].[Clusters] ([Id])
)
