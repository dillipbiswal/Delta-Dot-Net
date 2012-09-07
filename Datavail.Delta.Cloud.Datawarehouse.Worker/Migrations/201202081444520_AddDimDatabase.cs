namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimDatabase : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimDatabases",
                c => new
                    {
                        DatabaseKey = c.Int(nullable: false, identity: true),
                        DatabaseId = c.Guid(nullable: false),
                        InstanceKey = c.Int(nullable: false),
                        InstanceId = c.Guid(nullable: false),
                        Name = c.String(),
                        Status = c.String(),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(),
                        IsRowCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.DatabaseKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimDatabases");
        }
    }
}
