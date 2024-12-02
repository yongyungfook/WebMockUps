using Microsoft.EntityFrameworkCore;
using WebMockUps.Models;

namespace WebMockUps.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; } 
        public DbSet<Log> Logs { get; set; }
    }
}
