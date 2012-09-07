CREATE TABLE [dbo].[Metrics](
	[Id]					[uniqueidentifier]	NOT NULL,
	[AdapterAssembly]		[nvarchar](max)		NOT NULL,
	[AdapterVersion]		[nvarchar](max)		NOT NULL,
	[AdapterClass]			[nvarchar](max)		NOT NULL,
	[Name]					[nvarchar](2048)	NOT NULL,
	[Status_Id]				[int]				NOT NULL DEFAULT 1,
	[MetricType_Id]			[int]				NOT NULL DEFAULT 1,
	[MetricThresholdType_Id][int]				NOT NULL DEFAULT 0,
	[DatabaseVersion_Id]	[int]				NOT NULL DEFAULT 0,
	CONSTRAINT [PK_Metrics] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [IDX_Metric_Name] UNIQUE ([Name])
)


