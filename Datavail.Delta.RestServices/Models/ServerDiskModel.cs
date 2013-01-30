using System;

namespace Datavail.Delta.RestServices.Models
{
    public class ServerDiskModel : ModelBase
    {
        public Guid Id { get; set; }
        public Guid ClusterId { get; set; }
        public string Label { get; set; }
        public string Path { get; set; }
        public Guid ServerId { get; set; }
        public long TotalBytes { get; set; }
    }
}