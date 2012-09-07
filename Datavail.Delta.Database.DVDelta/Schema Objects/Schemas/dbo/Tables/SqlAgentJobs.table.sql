CREATE TABLE [dbo].[SqlAgentJobs] (
	[Id]			UNIQUEIDENTIFIER	NOT NULL,
	[Name]			NVARCHAR (1024)		NOT NULL,
	[Instance_Id]	UNIQUEIDENTIFIER	NULL,
	[Status_Id]		[int]				NOT NULL DEFAULT 1,
	CONSTRAINT [PK_SqlAgentJobs] PRIMARY KEY CLUSTERED ([Id] ASC) 
	WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON),
	CONSTRAINT [FK_SqlAgentJobs_Instance] FOREIGN KEY([Instance_Id]) REFERENCES [dbo].[DatabaseInstances] ([Id]),
	CONSTRAINT [FK_SqlAgentJobs_Status] FOREIGN KEY([Status_Id]) REFERENCES [dbo].[Statuses] ([Id]),
	CONSTRAINT [Instance_SqlAgentJob_Unique_Name] UNIQUE ([Instance_Id],[Name])
);