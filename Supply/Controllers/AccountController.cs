using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Supply.Models;
using Supply.Models.DTO;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
                    FullName = user.Username,
                  //  UserName = user.Username,
                    Email = user.Email,
                    Mobile = user.Mobile,
                    //Active = false
                   

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
                if(user!=null && user.Active == true)
                {
                    //Payload
                    if( await _userManager.CheckPasswordAsync(user,login.Password) )
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
        //get all roles
        [HttpGet("[Action]")]
        public IActionResult GetAllRoles()
        {
            var roles = _roleManager.Roles.ToList();
            return Ok(roles);
        }


        // Get all users
        [HttpGet("[Action]")]
        //[Route("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers( string? search,bool? IsActive, int pageNumber = 1, int pageSize = 1)
        {
            IEnumerable<ApplicationUser> appuser;
            appuser = await _userManager.Users.ToListAsync();
            if (IsActive.HasValue)
            
            {
             appuser = await _userManager.Users.Where(u=>u.Active==IsActive.Value).ToListAsync();
            }
            
               
            if (!string.IsNullOrEmpty(search))
            {
                appuser =  await _userManager.Users.Where(u => u.FullName.ToLower().Contains(search)).ToListAsync();
            }
            // Apply pagination
            if(pageSize>0)
            {

                if(pageSize>100)
                {
                    pageSize=100;
                }
            appuser = _userManager.Users.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                }
            Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize };

            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination));
            return Ok(appuser);
        }
        [HttpGet("[Action]")]
        //[Route("GetAllUsers")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        // Add User to role
        [HttpPost("[Action]")]
        //[Route("AddUserToRole")]
        public async Task<IActionResult> AddUserToRole(string userid, string roleid)
        {
            var user = await _userManager.FindByIdAsync(userid);
            var role = await _roleManager.FindByIdAsync(roleid);

            if (user != null)
            {
                var result = await _userManager.AddToRoleAsync(user, role.Name);

                if (result.Succeeded)
                {
                  
                    return Ok(new { result = $"User {user.FullName} added to the {role.Name} role" });
                }
                else
                {
                   
                    return BadRequest(new { error = $"Error: Unable to add user {user.FullName} to the {role.Name} role" });
                }
            }

            // User doesn't exist
            return BadRequest(new { error = "Unable to find user" });
        }

        [HttpPost("[Action]")]
        public async Task<IActionResult> SetActiveStatus(SetStatusActive model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                return BadRequest(new { error = "User not found" });
            }

            user.Active = model.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { result = $"User {user.UserName} active status set to {model.IsActive}" });
            }

            return BadRequest(new { error = "Error updating user active status", details = result.Errors });
        }
    }

}

