using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models.Config
{
    public class CheckInsModel
    {
        public int MessageId { get; set; }
        public System.DateTime InsertTimestamp { get; set; }
        public Nullable<System.DateTime> LockExpirationTimestamp { get; set; }
        //public int DequeueCount { get; set; }
        //public byte[] Data { get; set; }
        public string msg { get; set; }
    }
}