namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FactSqlAgentJobStatus : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "FactSqlAgentJobStatus",
                c => new
                    {
                        FactKey = c.Int(nullable: false, identity: true),
                        JobId = c.String(),
                        Status = c.String(),
                        Message = c.String(),
                        RunDuration = c.String(),
                        StepId = c.Int(nullable: false),
                        StepName = c.String(),
                        DateKey = c.Int(nullable: false),
                        TimeKey = c.String(),
                        TenantKey = c.Int(nullable: false),
                        TenantId = c.Guid(nullable: false),
                        CustomerKey = c.Int(nullable: false),
                        CustomerId = c.Guid(),
                        ServerKey = c.Int(nullable: false),
                        ServerId = c.Guid(nullable: false),
                        InstanceKey = c.Int(nullable: false),
                        InstanceId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.FactKey);
            
        }
        
        public override void Down()
        {
            DropTable("FactSqlAgentJobStatus");
        }
    }
}
