namespace Datavail.Delta.Repository.EfWithMigrations.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddMultipleQueueAndAgentErrorTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ApiUris",
                c => new
                {
                    Id = c.Guid(nullable: false),
                    PlugInName = c.String(),
                    URIAddress = c.String(),
                    AgentServerId = c.String(),
                    CustomerId = c.Guid(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.OnDemandMetricInstances",
                c => new
                {
                    Id = c.Guid(nullable: false),
                    Data = c.String(),
                    Label = c.String(),
                    Status_Id = c.Int(nullable: false),
                    StatusFlag = c.String(),
                    Metric_Id = c.Guid(),
                    Server_Id = c.Guid(),
                    DatabaseInstance_Id = c.Guid(),
                    Database_Id = c.Guid(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Metrics", t => t.Metric_Id)
                .ForeignKey("dbo.Servers", t => t.Server_Id)
                .ForeignKey("dbo.DatabaseInstances", t => t.DatabaseInstance_Id)
                .ForeignKey("dbo.Databases", t => t.Database_Id)
                .Index(t => t.Metric_Id)
                .Index(t => t.Server_Id)
                .Index(t => t.DatabaseInstance_Id)
                .Index(t => t.Database_Id);

            CreateTable(
                "dbo.OnDemandConfigBuilders",
                c => new
                {
                    Id = c.Guid(nullable: false),
                    BeginDate = c.DateTime(nullable: false),
                    EndDate = c.DateTime(nullable: false),
                    User = c.String(),
                    StatusFlag = c.String(),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.Clusters", "AgentError_Id", c => c.Int(nullable: false));
            AddColumn("dbo.Customers", "AgentError_Id", c => c.Int(nullable: false));
            AddColumn("dbo.MetricConfigurations", "OnDemandMetricInstance_Id", c => c.Guid());
            AddColumn("dbo.MaintenanceWindows", "OnDemandMetricInstance_Id", c => c.Guid());
            AddColumn("dbo.Servers", "AgentError_Id", c => c.Int(nullable: false));
            AddColumn("dbo.Servers", "ConfigStatus", c => c.String());
            AddColumn("dbo.ServerGroups", "AgentError_Id", c => c.Int(nullable: false));
            AddForeignKey("dbo.MetricConfigurations", "OnDemandMetricInstance_Id", "dbo.OnDemandMetricInstances", "Id");
            AddForeignKey("dbo.MaintenanceWindows", "OnDemandMetricInstance_Id", "dbo.OnDemandMetricInstances", "Id");
            CreateIndex("dbo.MetricConfigurations", "OnDemandMetricInstance_Id");
            CreateIndex("dbo.MaintenanceWindows", "OnDemandMetricInstance_Id");
        }

        public override void Down()
        {
            DropIndex("dbo.OnDemandMetricInstances", new[] { "Database_Id" });
            DropIndex("dbo.OnDemandMetricInstances", new[] { "DatabaseInstance_Id" });
            DropIndex("dbo.OnDemandMetricInstances", new[] { "Server_Id" });
            DropIndex("dbo.OnDemandMetricInstances", new[] { "Metric_Id" });
            DropIndex("dbo.MaintenanceWindows", new[] { "OnDemandMetricInstance_Id" });
            DropIndex("dbo.MetricConfigurations", new[] { "OnDemandMetricInstance_Id" });
            DropForeignKey("dbo.OnDemandMetricInstances", "Database_Id", "dbo.Databases");
            DropForeignKey("dbo.OnDemandMetricInstances", "DatabaseInstance_Id", "dbo.DatabaseInstances");
            DropForeignKey("dbo.OnDemandMetricInstances", "Server_Id", "dbo.Servers");
            DropForeignKey("dbo.OnDemandMetricInstances", "Metric_Id", "dbo.Metrics");
            DropForeignKey("dbo.MaintenanceWindows", "OnDemandMetricInstance_Id", "dbo.OnDemandMetricInstances");
            DropForeignKey("dbo.MetricConfigurations", "OnDemandMetricInstance_Id", "dbo.OnDemandMetricInstances");
            DropColumn("dbo.ServerGroups", "AgentError_Id");
            DropColumn("dbo.Servers", "ConfigStatus");
            DropColumn("dbo.Servers", "AgentError_Id");
            DropColumn("dbo.MaintenanceWindows", "OnDemandMetricInstance_Id");
            DropColumn("dbo.MetricConfigurations", "OnDemandMetricInstance_Id");
            DropColumn("dbo.Customers", "AgentError_Id");
            DropColumn("dbo.Clusters", "AgentError_Id");
            DropTable("dbo.OnDemandConfigBuilders");
            DropTable("dbo.OnDemandMetricInstances");
            DropTable("dbo.ApiUris");
        }
    }
}
