namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimTime : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimTimes",
                c => new
                    {
                        TimeKey = c.String(nullable: false, maxLength: 6),
                        Hour = c.Int(nullable: false),
                        Minute = c.Int(nullable: false),
                        Second = c.Int(nullable: false),
                        IsMorning = c.Boolean(nullable: false),
                        IsAfternoon = c.Boolean(nullable: false),
                        IsEvening = c.Boolean(nullable: false),
                        AmPm = c.String(),
                        HourMinute24Hour = c.String(),
                        HourMinuteSecond24Hour = c.String(),
                        HourMinute12Hour = c.String(),
                        HourMinuteSecond12Hour = c.String(),
                    })
                .PrimaryKey(t => t.TimeKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimTimes");
        }
    }
}
