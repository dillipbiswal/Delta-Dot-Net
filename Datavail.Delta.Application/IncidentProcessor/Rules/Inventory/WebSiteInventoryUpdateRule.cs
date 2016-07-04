using System.Xml.Linq;
using Datavail.Delta.Application.Interface;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.Inventory
{
    public sealed class WebSiteInventoryUpdateRule : IncidentProcessorRule
    {
        public WebSiteInventoryUpdateRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Web Site Inventory";
            XmlMatchString = "WebSiteInventoryPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();
        }

        public override bool IsMatch()
        {
            return false;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            foreach (var website in dataCollection.Descendants("WebSite"))
            {
                // ReSharper disable PossibleNullReferenceException
                Guard.IsNotNull(website.Attribute("SiteName"), "SiteName");
                Guard.IsNotNull(website.Attribute("SiteStatus"), "SiteStatus");

                var siteName = website.Attribute("SiteName").Value;
                var siteStatus = website.Attribute("SiteStatus").Value;


                ServerService.UpdateServerWebSiteInventory(ServerId, siteName);
                // ReSharper restore PossibleNullReferenceException
            }
        }
    }
}
