CREATE TABLE [dbo].[Dim_Servers](
	[ServerKey] [int] IDENTITY(1,1) NOT NULL,
	[ServerId] [uniqueidentifier] NULL,
	[CustomerKey] [int] NULL,
	[Hostname] [nvarchar](1024)  NULL,
	[IpAddress] [nvarchar](20)  NULL,
	[Status] [nvarchar](20)  NULL,
	[AgentVersion] [nvarchar](20)  NULL,
	[RowStart] [datetime] NULL,
	[RowEnd] [datetime] NULL,
	[IsRowCurrent] [bit] NOT NULL CONSTRAINT [DF_Dim_Servers_IsRowCurrent]  DEFAULT ((0)),
	CONSTRAINT [PK_Servers] PRIMARY KEY CLUSTERED ([ServerKey] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_Dim_Servers_Dim_Customers] FOREIGN KEY([CustomerKey]) REFERENCES [dbo].[Dim_Customers] ([CustomerKey])
)


