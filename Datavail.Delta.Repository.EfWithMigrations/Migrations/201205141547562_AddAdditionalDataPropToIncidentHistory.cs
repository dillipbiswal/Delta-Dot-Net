namespace Datavail.Delta.Repository.EfWithMigrations.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAdditionalDataPropToIncidentHistory : DbMigration
    {
        public override void Up()
        {
            AddColumn("IncidentHistories", "AdditionalData", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("IncidentHistories", "AdditionalData");
        }
    }
}
