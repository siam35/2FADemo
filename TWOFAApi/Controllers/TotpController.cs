using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AspNetCore.Totp;
using AspNetCore.Totp.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using User.Management.API.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using User.Management.API.Models.Authentication.SignUp;
using User.Management.Service.Services;
using User.Management.Service.Models;
using User.Management.API.Models.Authentication.Login;
using System.Text;
using User.Management.API.Models.Authentication.LoginWithOTP;
using TWOFAApi.Models;
using User.Management.API.Models.Authentication.LoginWithTOTPModel;

namespace TWOFAApi.Controllers
{
    internal struct UserIdentity
    {
        public int Id { get; set; }
        public string AccountSecretKey { get; set; }
    }

    internal static class AuthProvider
    {
        public static UserIdentity GetUserIdentity()
        {
            return new UserIdentity()
            {
                Id = new Random().Next(0, 999),
                AccountSecretKey = Guid.NewGuid().ToString()
            };
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    public class TotpController : ControllerBase
    {
        private readonly ITotpGenerator _totpGenerator;
        private readonly ITotpSetupGenerator _totpQrGenerator;
        private readonly ITotpValidator _totpValidator;
        private readonly UserIdentity _userIdentity;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public TotpController(IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _totpGenerator = new TotpGenerator();
            _totpValidator = new TotpValidator(_totpGenerator);
            _totpQrGenerator = new TotpSetupGenerator();
          //  _totpValidator = new TotpValidator(_totpQrGenerator);
            _userIdentity = AuthProvider.GetUserIdentity();
            _configuration = configuration;
            _userManager = userManager;
        }

        string filePath = @"F:\2FA SJ\QRdemo.txt";
        [HttpGet("code")]
        public int GetCode()
        {
            System.IO.File.WriteAllText(filePath, String.Empty);
            System.IO.File.WriteAllText(filePath, _userIdentity.AccountSecretKey);
            return _totpGenerator.Generate(_userIdentity.AccountSecretKey);
        }



        [HttpGet("qr-code")]
        public QRImageModel GetQr()
        {
            System.IO.File.WriteAllText(filePath, String.Empty);
            //System.IO.File.WriteAllText(filePath, _userIdentity.AccountSecretKey);
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            System.IO.File.WriteAllText(filePath, authSigningKey.ToString());
            var qrCode = _totpQrGenerator.Generate(
                "TestCo",
                _userIdentity.Id.ToString(),
                authSigningKey.ToString()
            );


            //byte[] b = Convert.FromBase64String(qrCode.QrCodeImage.Substring(22));

            // return "data:image/png;base64," + Convert.ToBase64String(b);
            //return  Convert.ToBase64String(b);

            return new QRImageModel { url = qrCode.QrCodeImage };


            // return File(imgData, "image/png");
            //return imgData.ToString();
        }



        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddDays(2),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        //[HttpPost("validate")]
        //public bool Validate([FromBody] int code)
        //{

        //    //return _totpValidator.Validate(_userIdentity.AccountSecretKey, code);
        //    return _totpValidator.Validate(System.IO.File.ReadAllText(filePath), code,0);

        //}

        [HttpPost("validate")]
        public async Task<ActionResult> Validate(LoginWithTOTPModel loginWithTOTPModel)
        {
            var user = await _userManager.FindByNameAsync(loginWithTOTPModel.Username);
            //return _totpValidator.Validate(_userIdentity.AccountSecretKey, code);
            if (_totpValidator.Validate(System.IO.File.ReadAllText(filePath), loginWithTOTPModel.Code, 0))
            {
                if (user != null)
                {
                    var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
                    var userRoles = await _userManager.GetRolesAsync(user);
                    foreach (var role in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    var jwtToken = GetToken(authClaims);
                  //  System.IO.File.WriteAllText(filePath, String.Empty);

                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        expiration = jwtToken.ValidTo
                    });
                    //returning the token...

                }
            }
            return StatusCode(StatusCodes.Status404NotFound,
                new Response { Status = "Error", Message = $"Invalid Code" });

        }





    }
}
