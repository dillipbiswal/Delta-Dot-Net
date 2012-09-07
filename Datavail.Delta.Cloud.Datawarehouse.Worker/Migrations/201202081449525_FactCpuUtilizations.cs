namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FactCpuUtilizations : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "FactCpuUtilizations",
                c => new
                    {
                        FactKey = c.Int(nullable: false, identity: true),
                        PercentageCpuUsed = c.Double(nullable: false),
                        DateKey = c.Int(nullable: false),
                        TimeKey = c.String(),
                        TenantKey = c.Int(nullable: false),
                        TenantId = c.Guid(nullable: false),
                        CustomerKey = c.Int(nullable: false),
                        CustomerId = c.Guid(),
                        ServerKey = c.Int(nullable: false),
                        ServerId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.FactKey);
            
        }
        
        public override void Down()
        {
            DropTable("FactCpuUtilizations");
        }
    }
}
