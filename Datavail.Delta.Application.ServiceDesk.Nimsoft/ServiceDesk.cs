using Datavail.Delta.Application.Interface;
using Datavail.Delta.Infrastructure.Logging;
using System;

namespace Datavail.Delta.Application.ServiceDesk.Nimsoft
{
    public class ServiceDesk : IServiceDesk
    {
        private readonly IDeltaLogger _logger;

        public ServiceDesk(IDeltaLogger logger) { _logger = logger; }

        public string OpenIncident(string serviceDeskData)
        {
            throw new NotImplementedException();
        }

        public string UpdateIncident(string serviceDeskData)
        {
            throw new NotImplementedException();
        }

        public string GetIncidentStatus(string serviceDeskData)
        {
           throw new NotImplementedException();
        }
    }
}
