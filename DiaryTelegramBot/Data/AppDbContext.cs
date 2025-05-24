using Microsoft.EntityFrameworkCore;
using ReminderWorker.Data;
using User = DiaryTelegramBot.Models.User;

namespace DiaryTelegramBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Record> Messages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Remind>()
                .HasKey(r => r.Id); 
        }

    }

}
