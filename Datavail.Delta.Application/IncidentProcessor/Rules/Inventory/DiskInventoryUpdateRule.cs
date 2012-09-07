using System.Xml.Linq;
using Datavail.Delta.Application.Interface;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.Inventory
{
    public sealed class DiskInventoryUpdateRule : IncidentProcessorRule
    {
        public DiskInventoryUpdateRule(IIncidentService incidentService, XDocument dataCollection, IServerService serverService)
            : base(incidentService, dataCollection, serverService)
        {
            RuleName = "Disk Inventory";
            XmlMatchString = "DiskInventoryPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();

            PercentageTypeLabel = "n/a";
            ValueTypeLabel = "n/a";

            PercentageTypeValue = 0;
            ValueTypeValue = 0;
        }

        public override bool IsMatch()
        {
            return false;
        }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            foreach (var disk in dataCollection.Descendants("Disk"))
            {
                // ReSharper disable PossibleNullReferenceException
                Guard.IsNotNull(disk.Attribute("isClusterDisk"), "isClusterDisk");
                Guard.IsNotNull(disk.Attribute("path"), "path");
                Guard.IsNotNull(disk.Attribute("label"), "label");
                Guard.IsNotNull(disk.Attribute("totalBytes"), "totalBytes");

                var isClusterDisk = bool.Parse(disk.Attribute("isClusterDisk").Value);
                var drivePath = disk.Attribute("path").Value;
                var label = disk.Attribute("label").Value;
                var totalBytes = long.Parse(disk.Attribute("totalBytes").Value);

                if (isClusterDisk)
                {
                    Guard.IsNotNull(disk.Attribute("clusterName"), "clusterName");
                    Guard.IsNotNull(disk.Attribute("resourceGroupName"), "resourceGroupName");

                    var clusterName = disk.Attribute("clusterName").Value;
                    var resourceGroupName = disk.Attribute("resourceGroupName").Value;
                    ServerService.UpdateClusterDiskInventory(ServerId, clusterName, resourceGroupName, drivePath, label, totalBytes);
                }
                else
                {
                    ServerService.UpdateServerDiskInventory(ServerId, drivePath, label, totalBytes);
                }
                // ReSharper restore PossibleNullReferenceException
            }
        }
    }
}
