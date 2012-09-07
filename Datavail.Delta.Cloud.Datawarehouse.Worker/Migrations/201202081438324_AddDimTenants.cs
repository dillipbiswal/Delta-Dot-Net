namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimTenants : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimTenants",
                c => new
                    {
                        TenantKey = c.Int(nullable: false, identity: true),
                        TenantId = c.Guid(nullable: false),
                        Name = c.String(),
                        Status = c.String(),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(),
                        IsRowCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.TenantKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimTenants");
        }
    }
}
