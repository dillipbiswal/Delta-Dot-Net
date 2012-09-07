CREATE TABLE [dbo].[DatabaseInstances](
	[Id]					UNIQUEIDENTIFIER	NOT NULL,
	[Name]					NVARCHAR (1024)		NULL,
	[Password]				NVARCHAR (1024)		NULL,
	[UseIntegratedSecurity]	BIT					NOT NULL DEFAULT 1,
	[Username]				NVARCHAR(255)		NULL,
	[Server_Id]				UNIQUEIDENTIFIER	NULL,
	[Status_Id]				[int]				NOT NULL DEFAULT 1,
	[DatabaseVersion_Id]	[int]				NOT NULL DEFAULT 0,
	CONSTRAINT [PK_DatabaseInstances] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_DatabaseInstance_Server] FOREIGN KEY([Server_Id]) REFERENCES [dbo].[Servers] ([Id]),
	CONSTRAINT [FK_DatabaseInstances_Status] FOREIGN KEY([Status_Id]) REFERENCES [dbo].[Statuses] ([Id])
)