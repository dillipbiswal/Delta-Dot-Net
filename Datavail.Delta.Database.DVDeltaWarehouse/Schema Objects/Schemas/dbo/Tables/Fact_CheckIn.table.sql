CREATE TABLE [dbo].[Fact_CheckIn](
	[FactKey] [int] IDENTITY (1, 1) NOT NULL,
	[DateKey] [int] NOT NULL,
	[TimeKey] [char](6)  NOT NULL,
	[CustomerKey] [int] NOT NULL,
	[TenantKey] [int] NOT NULL,
	[ServerKey] [int] NOT NULL,
	CONSTRAINT [PK_Fact_CheckIn] PRIMARY KEY CLUSTERED ([FactKey] DESC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
)


