namespace Datavail.Delta.Datawarehouse.Processor.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddServerDimension : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimServers",
                c => new
                    {
                        ServerKey = c.Int(nullable: false, identity: true),
                        ServerId = c.Guid(nullable: false),
                        CustomerKey = c.Int(nullable: false),
                        CustomerId = c.Guid(nullable: false),
                        AgentVersion = c.String(),
                        Hostname = c.String(),
                        IpAddress = c.String(),
                        IsVirtual = c.Boolean(nullable: false),
                        Status = c.String(),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(nullable: false),
                        IsRowCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ServerKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimServers");
        }
    }
}
