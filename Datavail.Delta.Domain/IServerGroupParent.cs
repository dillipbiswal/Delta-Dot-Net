using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public interface IServerGroupParent
    {
        Guid Id { get; set; }
        StatusWrapper Status { get; set; }
        IList<ServerGroup> ServerGroups { get; set; }
    }
}