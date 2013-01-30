using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public interface IServerGroupParent
    {
        Guid Id { get; set; }
        Status Status { get; set; }
        IList<ServerGroup> ServerGroups { get; set; }
    }
}