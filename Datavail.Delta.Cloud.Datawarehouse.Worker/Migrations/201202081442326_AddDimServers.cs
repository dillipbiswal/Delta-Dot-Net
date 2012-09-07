namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimServers : DbMigration
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
                        CustomerId = c.Guid(),
                        Hostname = c.String(),
                        IpAddress = c.String(),
                        Status = c.String(),
                        AgentVersion = c.String(),
                        IsVirtual = c.Boolean(nullable: false),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(),
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
