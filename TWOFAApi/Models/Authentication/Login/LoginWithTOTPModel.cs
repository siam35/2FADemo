using System.ComponentModel.DataAnnotations;

namespace User.Management.API.Models.Authentication.LoginWithTOTPModel
{
    public class LoginWithTOTPModel
    {
        [Required(ErrorMessage = "User Name is required")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Code is required")]
        public int Code { get; set; }
    }
}
