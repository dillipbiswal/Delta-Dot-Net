using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class ChangePasswordModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}