using System;
using System.Data.Entity.Migrations;
using Datavail.Delta.Cloud.Datawarehouse.Worker.Dimensions;

namespace Datavail.Delta.Cloud.Datawarehouse.Worker.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<DeltaDwContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(DeltaDwContext context)
        {
            //SeedDates(context);
            //SeedTimes(context);
        }

        private static void SeedTimes(DeltaDwContext context)
        {
            var currentTime = new DateTime(1900, 1, 1, 23, 48, 0);
            var endTime = new DateTime(1900, 1, 1, 23, 59, 59);

            var counter = 1;
            while (currentTime <= endTime)
            {
                var entry = DimTime.CreateTimeDimensionEntry(currentTime.Hour, currentTime.Minute, currentTime.Second);
                context.DimTimes.AddOrUpdate(e => e.TimeKey, entry);
                currentTime = currentTime.AddSeconds(1);

                if (counter == 100)
                {
                    context.SaveChanges();
                    counter = 1;
                }
                else
                {
                    counter++;
                }
            }

            context.SaveChanges();
        }

        private static void SeedDates(DeltaDwContext context)
        {
            var currentDate = new DateTime(2007, 1, 1);
            var endDate = new DateTime(2020, 12, 31);

            var counter = 1;
            while (currentDate <= endDate)
            {
                var entry = DimDate.CreateDateDimensionEntry(currentDate);
                context.DimDates.AddOrUpdate(e => e.DateKey, entry);
                currentDate = currentDate.AddDays(1);

                if (counter == 100)
                {
                    context.SaveChanges();
                    counter = 1;
                }
                else
                {
                    counter++;
                }
            }

            context.SaveChanges();
        }
    }
}
