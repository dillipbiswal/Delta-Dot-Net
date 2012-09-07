CREATE TABLE [dbo].[UserRoles](
	[User_Id] [uniqueidentifier] NOT NULL,
	[Role_Id] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([User_Id] ASC, [Role_Id] ASC
	)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_UserRoles_User] FOREIGN KEY([User_Id]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_UserRoles_Role] FOREIGN KEY([Role_Id]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE
)


