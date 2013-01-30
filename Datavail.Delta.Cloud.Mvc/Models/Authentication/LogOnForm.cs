
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Cloud.Mvc.Models.Authentication
{
    public class LogOnForm
    {
        [Required]
        [Microsoft.Web.Mvc.EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}