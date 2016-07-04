using System.Xml.Linq;
using Datavail.Delta.Application.Interface;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.Inventory
{
    public sealed class AppPoolInventoryUpdateRule : IncidentProcessorRule
    {
        public AppPoolInventoryUpdateRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Application Pool Inventory";
            XmlMatchString = "ApplicationPoolInventoryPluginOutput";

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
            foreach (var appPool in dataCollection.Descendants("ApplicationPool"))
            {
                // ReSharper disable PossibleNullReferenceException
                Guard.IsNotNull(appPool.Attribute("AppPoolName"), "AppPoolName");
                Guard.IsNotNull(appPool.Attribute("AppPoolStatus"), "AppPoolStatus");

                var appPoolName = appPool.Attribute("AppPoolName").Value;
                var appPoolStatus = appPool.Attribute("AppPoolStatus").Value;


                ServerService.UpdateServerAppPoolInventory(ServerId, appPoolName);
                // ReSharper restore PossibleNullReferenceException
            }
        }
    }
}
