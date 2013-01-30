using System;

namespace Datavail.Delta.RestServices.Models
{
    public class DatabaseInstanceModel : ModelBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public Guid ServerId { get; set; }
        public int StatusId { get; set; }
        public int DatabaseVersionId { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public string Username { get; set; }
    }
}