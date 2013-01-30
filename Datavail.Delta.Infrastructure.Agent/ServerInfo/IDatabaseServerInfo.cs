namespace Datavail.Delta.Infrastructure.Agent.ServerInfo
{
    public interface IDatabaseServerInfo
    {
        string Product { get; set; }
        string ProductVersion { get; set; }
        string ProductLevel { get; set; }
        string ProductEdition { get; set; }
    }
}