CREATE TABLE [dbo].[ServerGroupServers](
	[ServerGroup_Id] [uniqueidentifier] NOT NULL,
	[Server_Id] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_ServerGroupServers] PRIMARY KEY CLUSTERED ([ServerGroup_Id] ASC, [Server_Id] ASC
	)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_ServerGroupServers_ServerGroup] FOREIGN KEY([ServerGroup_Id]) REFERENCES [dbo].[ServerGroups] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ServerGroupServers_Server] FOREIGN KEY([Server_Id]) REFERENCES [dbo].[Servers] ([Id]) ON DELETE CASCADE
)


