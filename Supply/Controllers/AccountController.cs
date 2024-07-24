using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        public AccountController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IConfiguration configuration)
        {

            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        [HttpPost("[Action]")]
        public async Task<IActionResult> Register(Registerdto user)
        {

            var userExist = await _userManager.FindByNameAsync(user.Username);
            if (userExist != null)
                return BadRequest(new { error = "User already exists" });

            if (ModelState.IsValid)
            {
                ApplicationUser appuser = new()
                {
                    FullName = user.FullName,
                   UserName = user.Username,
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
        public async Task<IActionResult> CreateRole(Role role)
        {
            var roleExist = await _roleManager.RoleExistsAsync(role.NameEnglishRole);
            if (!roleExist)
            {
                //create the roles and seed them to the database
                var roleDB = new ApplicationRole
                {
                    Name = role.NameEnglishRole,
                    NameArablic = role.NameArabicRole
                };
                var roleResult = await _roleManager.CreateAsync(roleDB);

                if (roleResult.Succeeded)
                {

                    return Ok(new { result = $"Role {role.NameEnglishRole} added successfully" });
                }
                else
                {

                    return BadRequest(new { error = $"Issue adding the new {role.NameEnglishRole} role" });
                }
            }

            return BadRequest(new { error = "Role already exists" });
        }
        //get all roles
        [HttpGet("[Action]")]
        public IActionResult GetAllRoles()
        {
            var roles = _roleManager.Roles.Select(x=>new { x.Id, x.Name, x.NameArablic }).ToList();
            return Ok(roles);
        }


        // Get all users
        [HttpGet("[Action]")]
        //[Route("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers( string? search,bool? IsActive, int pageNumber = 1, int pageSize = 1)
        {
            IEnumerable<ApplicationUser> appuser;
            //appuser = await _userManager.Users.ToListAsync();
            //  appuser = _userManager.Users.Skip((pageNumber - 1) * pageSize).Take(pageSize);
           
                if (pageSize > 0)
                {

                    if (pageSize > 100)
                    {
                        pageSize = 100;
                    }
                    appuser = _userManager.Users.Skip((pageNumber - 1) * pageSize).Take(pageSize);



                    if (IsActive.HasValue)

                    {
                        appuser = await _userManager.Users.Where(u => u.Active == IsActive.Value).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                    }


                    if (!string.IsNullOrEmpty(search))
                    {
                        appuser = await _userManager.Users.Where(u => u.UserName.ToLower().Contains(search)).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                    }

                   
                    //total items
                    int totalItems = await _userManager.Users.CountAsync();
                    //  pagination response
                    Pagination pagination = new()
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)

                    };


                    Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination));
                    return Ok(appuser);
                }
            return BadRequest(new { error = "Unable to find user" });
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

       

        [HttpPost("[Action]")]

        public async Task<IActionResult> AddClaimToUser(string username, string claimName, string value)
        {
            var user = await _userManager.FindByNameAsync(username);

            var userClaim = new Claim(claimName, value);

            if (user != null)
            {
                var result = await _userManager.AddClaimAsync(user, userClaim);

                if (result.Succeeded)
                {
                   // _logger.LogInformation(1, $"the claim {claimName} add to the  User {user.Email}");
                    return Ok(new { result = $"the claim {claimName} add to the  User {user.Email}" });
                }
                else
                {
                   // _logger.LogInformation(1, $"Error: Unable to add the claim {claimName} to the  User {user.Email}");
                    return BadRequest(new { error = $"Error: Unable to add the claim {claimName} to the  User {user.Email}" });
                }
            }

            // User doesn't exist
            return BadRequest(new { error = "Unable to find user" });
        }

        [HttpGet("[Action]")]
        public async Task<IActionResult> GetAllClaims(string Username)
        {
            ApplicationUser? user = await _userManager.FindByNameAsync(Username);
            var claims = new List<Claim>();
            //  claims.Add(new Claim("tokenNo", "75"));
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsDto = claims.Select(c => new { c.Type, c.Value }).ToList();

            return Ok(claimsDto);
            
        }
    }
}



