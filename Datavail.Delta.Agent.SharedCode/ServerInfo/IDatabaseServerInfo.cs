namespace Datavail.Delta.Agent.SharedCode.ServerInfo
{
    public interface IDatabaseServerInfo
    {
        string Product { get; set; }
        string ProductVersion { get; set; }
        string ProductLevel { get; set; }
        string ProductEdition { get; set; }
    }
}