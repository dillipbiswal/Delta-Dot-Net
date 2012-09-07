using System;

namespace Datavail.Delta.Infrastructure.Agent
{
    public interface IPlugin
    {
        void Execute(Guid metricInstance, string label, string data);
    }
}
