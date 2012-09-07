namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FactRam : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "FactRams",
                c => new
                    {
                        FactKey = c.Int(nullable: false, identity: true),
                        TotalPhysicalMemoryBytes = c.Double(nullable: false),
                        TotalPhysicalMemoryFriendly = c.String(),
                        TotalVirtualMemoryBytes = c.Double(nullable: false),
                        TotalVirtualMemoryFriendly = c.String(),
                        AvailablePhysicalMemoryBytes = c.Double(nullable: false),
                        AvailablePhysicalMemoryFriendly = c.String(),
                        AvailableVirtualMemoryBytes = c.Double(nullable: false),
                        AvailableVirtualMemoryFriendly = c.String(),
                        PercentagePhysicalMemoryAvailable = c.Double(nullable: false),
                        PercentageVirtualMemoryAvailable = c.Double(nullable: false),
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
            DropTable("FactRams");
        }
    }
}
