CREATE TABLE [dbo].[Fact_Cpu]
(
	[FactKey] [int] IDENTITY(1,1) NOT NULL,
	[DateKey] [int] NULL,
	[TimeKey] [char](6) NULL,
	[CustomerKey] [int] NULL,
	[TenantKey] [int] NULL,
	[ServerKey] [int] NULL,
	[PercentageCpuUsed] [float] NULL,
		CONSTRAINT [PK_Fact_Cpu] PRIMARY KEY CLUSTERED 
	(
		[FactKey] ASC
	)
	WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)