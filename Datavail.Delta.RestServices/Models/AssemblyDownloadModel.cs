using System;

namespace Datavail.Delta.RestServices.Models
{
    public class AssemblyDownloadModel
    {
        public string AssemblyName { get; set; }
        public string Version { get; set; }
        public DateTime Timestamp { get; set; }
        public string GeneratingServer { get; set; }
        public byte[] Contents { get; set; }
    }
}