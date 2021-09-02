using Live.Identity.API.Extensions;
using Live.Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Live.Identity.API.Controllers
{
    [Route("api/v1/identity")]
    public class AuthController : MainController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppSettings _appSettings;

        public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, AppSettings appSettings)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appSettings = appSettings;
        }

        [HttpPost("new-account")]
        public async Task<ActionResult> Register(UserRecord userRecord)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var user = new IdentityUser
            {
                UserName = userRecord.Email,
                Email = userRecord.Email,
                EmailConfirmed = true  //[D1] - should we confirm the email??
            };

            var result = await _userManager.CreateAsync(user, userRecord.Password);

            if (result.Succeeded)
            {
                return CustomResponse(await GenerateJwt(userRecord.Email));
            }

            foreach (var error in result.Errors)
            {
                AddErrorToProcess(error.Description);
            }

            return CustomResponse();
        }

        [HttpPost("authenticate")]
        public async Task<ActionResult> Login(UserRecord userRecord)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var result = await _signInManager.PasswordSignInAsync(userRecord.Email, userRecord.Password,
                false, true);

            if (result.Succeeded)
            {
                return CustomResponse(await GenerateJwt(userRecord.Email));
            }

            if (result.IsLockedOut)
            {
                AddErrorToProcess("Your account has been temporarily locked due multiple invalid login attempts");
                return CustomResponse();
            }

            AddErrorToProcess("User or Password incorrect");
            return CustomResponse();
        }

        private async Task<UserLoginResponse> GenerateJwt(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var claims = await _userManager.GetClaimsAsync(user);

            var identityClaims = await ObtainUserClaims(claims, user);
            var encodedToken = GenerateToken(identityClaims);

            return ObtainTokenResponse(encodedToken, user, claims);
        }

        private UserLoginResponse ObtainTokenResponse(string encodedToken, IdentityUser user, IList<Claim> claims)
        {
            return new UserLoginResponse
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(_appSettings.ExpirationTime).TotalSeconds,//[D2]
                UserToken = new UserToken
                {
                    Id = user.Id,
                    Email = user.Email,
                    Claims = claims.Select(c => new UserClaim { Type = c.Type, Value = c.Value })
                }
            };
        }

        private async Task<ClaimsIdentity> ObtainUserClaims(ICollection<Claim> claims, IdentityUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("role", userRole));
            }

            var identityClaims = new ClaimsIdentity();
            identityClaims.AddClaims(claims);

            return identityClaims;
        }
        private string GenerateToken(ClaimsIdentity identityClaims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Issuer,
                Audience = _appSettings.Audience,
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(_appSettings.ExpirationTime),//[D2] - We have to define
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });
            return tokenHandler.WriteToken(token);
        }

        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }
}
