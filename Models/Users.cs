using Microsoft.AspNetCore.Identity;

namespace SmartHome.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
