using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class IjazahDbContext: DbContext
    {
        public IjazahDbContext(DbContextOptions<IjazahDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
