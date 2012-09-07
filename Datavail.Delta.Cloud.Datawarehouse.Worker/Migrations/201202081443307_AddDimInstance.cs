namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimInstance : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimInstances",
                c => new
                    {
                        InstanceKey = c.Int(nullable: false, identity: true),
                        InstanceId = c.Guid(nullable: false),
                        ServerKey = c.Int(nullable: false),
                        ServerId = c.Guid(nullable: false),
                        Name = c.String(),
                        Status = c.String(),
                        DatabaseVersion = c.String(),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(),
                        IsRowCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.InstanceKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimInstances");
        }
    }
}
