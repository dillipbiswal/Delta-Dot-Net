namespace Datavail.Delta.Repository.EfWithMigrations.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddWebsiteData : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.WebSiteDatas",
                c => new
                {
                    Id = c.Guid(nullable: false),
                    Label = c.String(),
                    Path = c.String(),
                    Identity = c.String(),
                    Server_Id = c.Guid(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Servers", t => t.Server_Id)
                .Index(t => t.Server_Id);

            AddColumn("dbo.Servers", "LastConfigBuild", c => c.DateTime(nullable: false));
        }

        public override void Down()
        {
            DropIndex("dbo.WebSiteDatas", new[] { "Server_Id" });
            DropForeignKey("dbo.WebSiteDatas", "Server_Id", "dbo.Servers");
            DropColumn("dbo.Servers", "LastConfigBuild");
            DropTable("dbo.WebSiteDatas");
        }
    }
}
