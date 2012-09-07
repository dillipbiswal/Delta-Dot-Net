using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class AddServerModel
    {
        public Guid ParentId { get; set; }
        public IEnumerable<Server> PotentialServers { get; set; }
        public IEnumerable<SelectListItem> PotentialServersSelectListItems { get; set; } 
        public IEnumerable<Guid> SelectedServerIds { get; set; }
        public string SelectedServersHeader { get; set; }
        public string PotentialServersHeader { get; set; }
        public string UnavailableMessage { get; set; }

        public AddServerModel(IEnumerable<Server> potentialServers, IEnumerable<SelectListItem> potentialServersSelectListItems, IEnumerable<Guid> selectedServerIds, Guid parentId)
        {
            ParentId = parentId;
            PotentialServers = potentialServers;
            PotentialServersSelectListItems = potentialServersSelectListItems;
            SelectedServerIds = selectedServerIds;
        }

        public AddServerModel(IEnumerable<Server> potentialServers, IEnumerable<Guid> selectedServerIds, Guid parentId)
        {
            ParentId = parentId;
            PotentialServers = potentialServers;
            SelectedServerIds = selectedServerIds;
        }

        public AddServerModel()
        {
            SelectedServerIds = new List<Guid>();
        }
    }
}