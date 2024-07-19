using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Supply.Models;
using Supply.Models.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Supply.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        public AccountController(UserManager<ApplicationUser> userManager,IConfiguration configuration)
        {

            _userManager = userManager;
            _configuration = configuration;
        }
        [HttpPost("[Action]")]
        public async Task<IActionResult> Register(Registerdto user)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser appuser = new()
                {
                    Name= user.Username,
                    UserName=user.Username,
                    Email = user.Email,
                    Mobile = user.Mobile,

                };
                IdentityResult result = await _userManager.CreateAsync(appuser, user.Password);
                if (result.Succeeded)
                {
                    return Ok("sucess");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError("", item.Description);
                    }
                }
            }
            return BadRequest(ModelState);
        }

        // [HttpPost("[Login]")]
        [HttpPost("[Action]")]
        public async Task<IActionResult> Login(Logindto login)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser? user = await _userManager.FindByNameAsync(login.Username);
                if(user!=null)
                {
                    //Payload
                    if( await _userManager.CheckPasswordAsync(user,login.Password))
                    {
                        var claims = new List<Claim>();
                      //  claims.Add(new Claim("tokenNo", "75"));
                        claims.Add(new Claim(ClaimTypes.Name,user.UserName));
                        claims.Add(new Claim(ClaimTypes.NameIdentifier,user.Id));
                        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                        var roles= await _userManager.GetRolesAsync(user);
                        foreach (var role in roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role,role.ToString()));
                        }
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
                        var SigningCredential = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
                        var token = new JwtSecurityToken(
                           claims: claims,
                            issuer: _configuration["JWT:issuer"],
                            audience: _configuration["JWT:Audience"],
                            expires: DateTime.UtcNow.AddDays(int.Parse(_configuration["JWT:DurationInDays"])),
                            signingCredentials: SigningCredential
                            );
                        var _token=new
                        {
                            token=new JwtSecurityTokenHandler().WriteToken(token),
                            expiration = token.ValidTo
                        };
                        return Ok(_token);
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ModelState.AddModelError("", "User Name is valid");
                }
              
            }
            return BadRequest(ModelState);
        }
    }
}
