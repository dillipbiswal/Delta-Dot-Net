using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Datavail.Delta.Repository.Ef.Infrastructure;

namespace Datavail.Delta.WebServices
{
    public class Global : System.Web.HttpApplication
    {
        private IDbContextStorage _storage;

        public override void Init()
        {
            base.Init();
            _storage = new WebDbContextStorage(this);
        }


        protected void Application_Start(object sender, EventArgs e)
        {

        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            DbContextInitializer.Instance().InitializeObjectContextOnce(() =>
            {
                DbContextManager.InitStorage(_storage);
                DbContextManager.Init("DeltaConnectionString", new[] { Server.MapPath("~/bin/Datavail.Delta.Repository.Ef.dll") }, false, true);
            });
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}