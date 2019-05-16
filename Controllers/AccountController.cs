using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NG_Core_Auth.Helpers;
using NG_Core_Auth.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text;

namespace NG_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    public class AccountController:Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly SignInManager<IdentityUser> _signManager;

        private readonly AppSettings _appSetting;

        public AccountController(UserManager<IdentityUser> userManager,SignInManager<IdentityUser> signInManager,
            IOptions<AppSettings> appSettings)
        {
            _userManager = userManager;
            _signManager = signInManager;

            //IOption Declaration -> For access data in class
            _appSetting = appSettings.Value;
        }
        
        [HttpPost("[action]")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel formdata)
        {
            // will hold all the errors related to registration
            List<string> errorList = new List<string>();

            var user = new IdentityUser
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = formdata.Username,
                Email = formdata.Email                
            };

            var result = await _userManager.CreateAsync(user, formdata.Password);

            if(result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                // Sending Confirmation Email
                return Ok(new { username = user.UserName, email = user.Email, status = 1, message = "Registeration Successful!" });
            }   
            else
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    errorList.Add(error.Description);
                }
            }

            return BadRequest(new JsonResult(errorList));

        }

        // Login Method
        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody]LoginViewModel formdata)
        {
            // Get the User from database
            var user = await _userManager.FindByNameAsync(formdata.Username);

            var roles = await _userManager.GetRolesAsync(user);

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSetting.Secret));

            double tokenExpiryTime = Convert.ToDouble(_appSetting.ExpireTime);

            if(user!=null && await _userManager.CheckPasswordAsync(user,formdata.Password))
            {
                // Confirmation of email 

                var tokenHandler = new JwtSecurityTokenHandler();

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub,formdata.Username),
                        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.NameIdentifier,user.Id),
                        new Claim(ClaimTypes.Role,roles.FirstOrDefault()),
                        new Claim("LoggedOn",DateTime.Now.ToString()),
                    }),
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _appSetting.Site,
                    Audience = _appSetting.Audience,
                    Expires = DateTime.UtcNow.AddMinutes(tokenExpiryTime)
                };

                // Generate Token
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return Ok(new { token = tokenHandler.WriteToken(token), expiration = token.ValidTo,
                                username = user.UserName, useRole = roles.FirstOrDefault() });


            }

            // return error
            ModelState.AddModelError("", "Username/Password was not found!");
            return Unauthorized(new { LoginError = "Please Check the Login Creditials - Invalid Username/Password was enterd!" });
        }

        
    }
}
