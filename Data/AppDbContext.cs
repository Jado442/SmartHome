using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHome.Models;

namespace SmartHome.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
         
    }
        public DbSet<UnauthorizedAccessAttempt> UnauthorizedAccessAttempts { get; set; }
    }
}
