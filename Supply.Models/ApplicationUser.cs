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
        public string Name { get; set; }
        public string Mobile { get; set; }
    }
}
