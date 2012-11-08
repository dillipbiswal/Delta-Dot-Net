using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Util;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.Inventory
{
    public sealed class DatabaseInventoryUpdateRule : IncidentProcessorRule
    {
        private readonly List<string> _databaseNames;
        private Guid _instanceId;

        public DatabaseInventoryUpdateRule( IIncidentService incidentService, XDocument dataCollection,
                                      IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            _databaseNames = new List<string>();
            RuleName = "Database Inventory Update";
            XmlMatchString = "DatabaseInventoryPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();
            MatchTypeValue = string.Empty;
        }

        private void UpdateDomainEntities() { ServerService.UpdateInstanceDatabaseInventory(_instanceId, _databaseNames); }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            // ReSharper disable PossibleNullReferenceException
            Guard.IsNotNull(dataCollection.Root.Attribute("instanceId"), "instanceId");

            _instanceId = Guid.Parse(dataCollection.Root.Attribute("instanceId").Value);

            foreach (var database in dataCollection.Descendants("Database"))
            {
                _databaseNames.Add(database.Attribute("name").Value);
            }
            // ReSharper restore PossibleNullReferenceException

            UpdateDomainEntities();
        }

        public override bool IsMatch()
        {
            return false;
        }
    }
}