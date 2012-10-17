using System;

namespace Datavail.Delta.RestServices.Models
{
    public abstract class ModelBase
    {
        public string GeneratingServer { get; set; }
        public DateTime Timestamp { get; set; }

        protected ModelBase()
        {
            GeneratingServer = Environment.MachineName;
            Timestamp = DateTime.UtcNow;
        }
    }
}