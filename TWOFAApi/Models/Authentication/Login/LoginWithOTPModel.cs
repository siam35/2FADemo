using System.ComponentModel.DataAnnotations;

namespace User.Management.API.Models.Authentication.LoginWithOTP
{
    public class LoginWithOTPModel
    {
        [Required(ErrorMessage = "User Name is required")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Code is required")]
        public string? Code { get; set; }
        
        
    }
}
