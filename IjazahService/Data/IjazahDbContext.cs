using Microsoft.EntityFrameworkCore;
using IjazahService.Models;

namespace IjazahService.Data
{
    public class IjazahDbContext : DbContext
    {
        public IjazahDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Ijazah> Ijazahs { get; set; }
    }
}
