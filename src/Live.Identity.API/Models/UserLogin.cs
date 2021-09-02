using System.ComponentModel.DataAnnotations;

namespace Live.Identity.API.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [EmailAddress(ErrorMessage = "O campo {0} está em formato inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(100, ErrorMessage = "The field {0} needs to be between {2} and {1}", MinimumLength = 6)]
        public string Password { get; set; }
    }
}
