CREATE TABLE [dbo].[Fact_Ram]
(
	[FactKey] [int] IDENTITY(1,1) NOT NULL,
	[DateKey] [int] NULL,
	[TimeKey] [char](6) NULL,
	[CustomerKey] [int] NULL,
	[TenantKey] [int] NULL,
	[ServerKey] [int] NULL,
	[TotalPhysicalMemoryBytes] [bigint] NULL,
	[TotalPhysicalMemoryFriendly] [nvarchar](50) NULL,
	[TotalVirtualMemoryBytes] [bigint] NULL,
	[TotalVirtualMemoryFriendly] [nvarchar](50) NULL,
	[AvailablePhysicalMemoryBytes] [bigint] NULL,
	[AvailablePhysicalMemoryFriendly] [nvarchar](50) NULL,
	[AvailableVirtualMemoryBytes] [bigint] NULL,
	[AvailableVirtualMemoryFriendly] [nvarchar](50) NULL,
	[PercentagePhysicalMemoryAvailable] [float] NULL,
	[PercentageVirtualMemoryAvailable] [float] NULL,
	CONSTRAINT [PK_Fact_Ram] PRIMARY KEY CLUSTERED 
	(
		[FactKey] ASC
	)
	WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)