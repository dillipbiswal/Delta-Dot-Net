
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Datavail.Delta.Cloud.Mvc.Models.SignUp
{
    public class SignUpForm
    {
        [Required]
        [Microsoft.Web.Mvc.EmailAddress]
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
        [System.Web.Mvc.Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string PasswordAgain { get; set; }
    }
}