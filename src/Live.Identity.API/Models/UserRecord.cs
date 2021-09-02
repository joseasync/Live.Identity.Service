using System.ComponentModel.DataAnnotations;

namespace Live.Identity.API.Models
{
    public class UserRecord
    {
        [Required(ErrorMessage = "The field {0} is required")]
        [EmailAddress(ErrorMessage = "The field {0} is invalid")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(100, ErrorMessage = "The field {0} needs to be between {2} and {1}", MinimumLength = 6)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords does not match.")]
        public string ConfirmedPassword { get; set; }
    }
}
