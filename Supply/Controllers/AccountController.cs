using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        public AccountController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {

            _userManager = userManager;
            _roleManager = roleManager;
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

        [HttpGet("[Action]")]
        public IActionResult GetAllRoles()
        {
            var roles = _roleManager.Roles.ToList();
            return Ok(roles);
        }

        [HttpPost("[Action]")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var roleExist = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                //create the roles and seed them to the database
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (roleResult.Succeeded)
                {
                   
                    return Ok(new { result = $"Role {roleName} added successfully" });
                }
                else
                {
                  
                    return BadRequest(new { error = $"Issue adding the new {roleName} role" });
                }
            }

            return BadRequest(new { error = "Role already exist" });
        }

        // Get all users
        [HttpGet("[Action]")]
        //[Route("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return Ok(users);
        }

        // Add User to role
        [HttpPost("[Action]")]
        //[Route("AddUserToRole")]
        public async Task<IActionResult> AddUserToRole(string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                  
                    return Ok(new { result = $"User {user.Email} added to the {roleName} role" });
                }
                else
                {
                   
                    return BadRequest(new { error = $"Error: Unable to add user {user.Email} to the {roleName} role" });
                }
            }

            // User doesn't exist
            return BadRequest(new { error = "Unable to find user" });
        }

    }
}
