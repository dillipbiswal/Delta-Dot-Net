namespace Datavail.Delta.Repository.EfWithMigrations.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddIncidentHistoryIncidentTimestamp : DbMigration
    {
        public override void Up()
        {
            AddColumn("IncidentHistories", "IncidentTimestamp", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("IncidentHistories", "IncidentTimestamp");
        }
    }
}
