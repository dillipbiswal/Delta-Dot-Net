namespace Datavail.Delta.Application.Interface
{
    public interface IServiceDesk
    {
        string OpenIncident(string serviceDeskData);
        string UpdateIncident(string serviceDeskData);
        string GetIncidentStatus(string serviceDeskData);
    }
}