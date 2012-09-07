namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimSqlAgentJob : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimSqlAgentJobs",
                c => new
                    {
                        SqlAgentJobKey = c.Int(nullable: false, identity: true),
                        SqlAgentJobId = c.Guid(nullable: false),
                        InstanceKey = c.Int(nullable: false),
                        InstanceId = c.Guid(nullable: false),
                        Name = c.String(),
                        Status = c.String(),
                        RowStart = c.DateTime(nullable: false),
                        RowEnd = c.DateTime(),
                        IsRowCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.SqlAgentJobKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimSqlAgentJobs");
        }
    }
}
