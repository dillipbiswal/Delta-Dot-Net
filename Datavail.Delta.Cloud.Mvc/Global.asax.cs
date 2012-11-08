using System;
using System.Data.Entity;
using Datavail.Delta.Cloud.Mvc.Infrastructure;
using Datavail.Delta.Repository.EfWithMigrations;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc
{
    public class MvcApplication : HttpApplication
    {
        public override void Init()
        {
            base.Init();
            ApplicationFramework.Bootstrap();
        }

        private static bool _started;
        protected void Application_BeginRequest(object sender, EventArgs e)
        {

            if (!_started)
            {
                ApplicationFramework.Start();
                _started = true;
            }
        }
    }
}