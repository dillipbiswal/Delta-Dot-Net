﻿CREATE TABLE [dbo].[Statuses]
(
	[Id] [int] NOT NULL,
	[Name] [nvarchar](255)  NOT NULL,
	CONSTRAINT [PK_Statuses] PRIMARY KEY CLUSTERED ([Id] ASC)
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
)
