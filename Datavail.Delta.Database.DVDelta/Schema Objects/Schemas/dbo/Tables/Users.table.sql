CREATE TABLE [dbo].[Users](
	[Id] [uniqueidentifier] NOT NULL,
	[EmailAddress] [nvarchar](500)  NULL,
	[FirstName] [nvarchar](50)  NULL,
	[LastName] [nvarchar](50)  NULL,
	[PasswordHash] [nvarchar](max)  NULL,
	[PasswordSalt] [nvarchar](max)  NULL,
	CONSTRAINT [PK_Users] PRIMARY KEY NONCLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [IDX_Users_EmailAddress] UNIQUE CLUSTERED ([EmailAddress] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
