using System.Collections.Generic;
using System.Xml.Linq;

namespace Datavail.Delta.Agent.SharedCode.Cluster
{
    public interface IClusterInfo
    {
        string GroupStatus { get; set; }
        string NodeStatus { get; set; }

        IEnumerable<XElement> GetClusterDisks();
        bool IsActiveClusterNodeForGroup(string clusterGroupName);
        
    }
}