using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Supply.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string? FullName { get; set; }


        public bool Active { get; set; }
        public ApplicationUser()
        {


            Active = false;
        }
        public string Mobile { get; set; }
    }
}
