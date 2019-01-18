using ExchangeBroker.Models;
using Graft.Infrastructure.AccountPool;
using GraftLib.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExchangeBroker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Payment> Payment { get; set; }
        public DbSet<Exchange> Exchange { get; set; }
        public DbSet<TransactionRequest> TransactionRequests { get; set; }
        public DbSet<AccountPoolItem> AccountPools { get; set; }
    }
}
