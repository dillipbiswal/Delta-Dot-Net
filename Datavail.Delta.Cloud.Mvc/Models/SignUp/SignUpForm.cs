using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Microsoft.Web.Mvc;

namespace Datavail.Delta.Cloud.Mvc.Models.SignUp
{
    public class SignUpForm
    {
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("Password (again)")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string PasswordAgain { get; set; }
    }
}