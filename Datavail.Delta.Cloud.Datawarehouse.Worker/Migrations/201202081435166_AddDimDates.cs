namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDimDates : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "DimDates",
                c => new
                    {
                        DateKey = c.Int(nullable: false),
                        DayOfWeek = c.String(),
                        WeekBeginDate = c.DateTime(nullable: false),
                        WeekNumber = c.Int(nullable: false),
                        MonthNumber = c.Int(nullable: false),
                        MonthName = c.String(),
                        MonthNameShort = c.String(),
                        MonthEndDate = c.DateTime(nullable: false),
                        DaysInMonth = c.Int(nullable: false),
                        YearMonth = c.Int(nullable: false),
                        QuarterNumber = c.Int(nullable: false),
                        QuarterName = c.String(),
                        Year = c.Int(nullable: false),
                        IsWeekend = c.Boolean(nullable: false),
                        IsWorkday = c.Boolean(nullable: false),
                        WeekendOrWeekday = c.String(),
                        IsHoliday = c.Boolean(nullable: false),
                        HolidayName = c.String(),
                    })
                .PrimaryKey(t => t.DateKey);
            
        }
        
        public override void Down()
        {
            DropTable("DimDates");
        }
    }
}
