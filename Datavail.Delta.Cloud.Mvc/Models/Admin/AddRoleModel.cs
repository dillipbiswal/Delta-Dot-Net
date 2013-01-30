using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Cloud.Mvc.Models.Admin
{
    public class AddRoleModel
    {
        public IEnumerable<Role> PotentialRoles { get; set; }
        public IEnumerable<Guid> SelectedRoleIds { get; set; }
        public string SelectedRolesHeader { get; set; }
        public string PotentialRolesHeader { get; set; }

        public AddRoleModel(IEnumerable<Role> potentialRoles, IEnumerable<Guid> selectedRoleIds)
        {
            PotentialRoles = potentialRoles;
            SelectedRoleIds = selectedRoleIds;
        }

        public AddRoleModel()
        {
            SelectedRoleIds = new List<Guid>();

        }
    }
}