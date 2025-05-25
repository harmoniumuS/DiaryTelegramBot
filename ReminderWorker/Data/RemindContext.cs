using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReminderWorker.Data
{
    public class RemindContext : DbContext
    {
        public RemindContext(DbContextOptions<RemindContext> options)
            : base(options)
        {
        }

        public DbSet<Remind> Reminds { get; set; }

        public async Task<IReadOnlyList<Remind>> ReadRemindsInTimespan(DateTime from, DateTime to) =>
            await Reminds
                .Where(r => !r.IsRemind && r.Time >= from && r.Time <= to)
                .ToListAsync();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Remind>().HasKey(r => r.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}