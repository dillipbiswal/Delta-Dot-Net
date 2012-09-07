CREATE TABLE [dbo].[Dim_Customers](
	[CustomerKey] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [uniqueidentifier] NULL,
	[Name] [nvarchar](1024)  NULL,
	[Status] [nvarchar](20)  NULL,
	[RowStart] [datetime] NULL,
	[RowEnd] [datetime] NULL,
	[IsRowCurrent] [bit] NOT NULL CONSTRAINT [DF_Dim_Customers_IsRowCurrent]  DEFAULT ((0)),
 CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED 
(
	[CustomerKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)


