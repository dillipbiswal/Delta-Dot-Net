namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FactDatabaseStatus : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "FactDatabaseStatus",
                c => new
                    {
                        FactKey = c.Int(nullable: false, identity: true),
                        Status = c.String(),
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
            DropTable("FactDatabaseStatus");
        }
    }
}
