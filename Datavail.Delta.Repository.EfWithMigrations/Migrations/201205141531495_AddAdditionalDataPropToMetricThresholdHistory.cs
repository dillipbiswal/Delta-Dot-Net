namespace Datavail.Delta.Repository.EfWithMigrations.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAdditionalDataPropToMetricThresholdHistory : DbMigration
    {
        public override void Up()
        {
            AddColumn("MetricThresholdHistories", "AdditionalData", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("MetricThresholdHistories", "AdditionalData");
        }
    }
}
