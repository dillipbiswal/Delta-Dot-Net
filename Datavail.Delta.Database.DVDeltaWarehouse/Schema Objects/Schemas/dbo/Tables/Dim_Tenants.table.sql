CREATE TABLE [dbo].[Dim_Tenants](
	[TenantKey] [int] IDENTITY(1,1) NOT NULL,
	[TenantId] [uniqueidentifier] NULL,
	[Name] [nvarchar](1024)  NULL,
	[Status] [nvarchar](20)  NULL,
	[RowStart] [datetime] NULL,
	[RowEnd] [datetime] NULL,
	[IsRowCurrent] [bit] NOT NULL CONSTRAINT [DF_Dim_Tenants_IsRowCurrent]  DEFAULT ((0)),
 CONSTRAINT [PK_Tenants] PRIMARY KEY CLUSTERED 
(
	[TenantKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)


