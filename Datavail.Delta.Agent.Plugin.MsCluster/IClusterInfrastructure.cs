namespace Datavail.Delta.Agent.Plugin.MsCluster
{
    public interface IClusterInfrastructure
    {
        string GetActiveNodeForGroup(string clusterGroupName);
        string GetClusterName();
        string GetClusterName(string hostname);
        string GetStatusForGroup(string clusterGroupName);
    }
}