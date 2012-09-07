namespace Datavail.Delta.Datawarehouse.Processor.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<DeltaDwContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(DeltaDwContext context)
        {
         
        }
    }
}
