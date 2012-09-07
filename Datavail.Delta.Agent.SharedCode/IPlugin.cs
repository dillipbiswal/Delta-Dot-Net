using System;

namespace Datavail.Delta.Agent.SharedCode
{
    public interface IPlugin
    {
        void Execute(Guid metricInstance, string label, string data);
    }
}
