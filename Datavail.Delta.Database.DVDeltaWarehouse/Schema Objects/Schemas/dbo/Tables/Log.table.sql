CREATE TABLE [dbo].[Log](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[Thread] [varchar](255)  NOT NULL,
	[Level] [varchar](50)  NOT NULL,
	[Hostname] [varchar](255)  NULL,
	[Logger] [varchar](255)  NOT NULL,
	[Message] [varchar](max)  NOT NULL,
	[Exception] [varchar](max)  NULL,
	CONSTRAINT [PK_Log] PRIMARY KEY NONCLUSTERED ([Id] ASC)	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [IDX_Log_Date] UNIQUE CLUSTERED ([Date] DESC)
)