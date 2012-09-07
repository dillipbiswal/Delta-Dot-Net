namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimCustomers : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimCustomers",
                c => new
                    {
                        CustomerKey = c.Int(nullable: false, identity: true),
                        CustomerId = c.Guid(nullable: false),
                        Name = c.String(),
                        Status = c.String(),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(),
                        IsRowCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CustomerKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimCustomers");
        }
    }
}
