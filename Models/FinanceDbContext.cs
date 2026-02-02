using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FinanceTracker.Models
{
    // The DbContext manages the database connection and tables
    public class FinanceDbContext : DbContext
    {
        public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

        // These represent your tables in the database
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SpendingSummary> SpendingSummaries { get; set; }
    }
}