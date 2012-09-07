using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Datavail.Delta.Application.Interface;

using Datavail.Delta.Infrastructure.Util;

namespace Datavail.Delta.Application.IncidentProcessor.Rules.Inventory
{
    public sealed class SqlAgentJobInventoryUpdateRule : IncidentProcessorRule
    {
        private readonly List<string> _jobNames;
        private Guid _instanceId;

        public SqlAgentJobInventoryUpdateRule( IIncidentService incidentService, XDocument dataCollection,
                                      IServerService serverService)
            : base( incidentService, dataCollection, serverService)
        {
            _jobNames = new List<string>();
            RuleName = "SqlAgentJob Inventory Update";
            XmlMatchString = "SqlAgentJobInventoryPluginOutput";

            SetupMatchParams();
        }

        protected override void SetupMatchParams()
        {
            base.SetupMatchParams();
            MatchTypeValue = string.Empty;
        }

        private void UpdateDomainEntities() { ServerService.UpdateInstanceSqlAgentJobInventory(_instanceId, _jobNames); }

        protected override void ParseDataCollection(XDocument dataCollection)
        {
            // ReSharper disable PossibleNullReferenceException
            Guard.IsNotNull(dataCollection.Root.Attribute("instanceId"), "instanceId");

            _instanceId = Guid.Parse(dataCollection.Root.Attribute("instanceId").Value);

            foreach (var job in dataCollection.Descendants("SqlAgentJob"))
            {
                _jobNames.Add(job.Attribute("name").Value);
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