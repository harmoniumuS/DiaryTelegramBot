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
        public DbSet<Record> Records { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Remind>()
                .HasKey(r => r.Id); 
            modelBuilder.Entity<User>()
                .Ignore(u => u.Messages)
                .HasMany<Record>("_messages")
                .WithOne()
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<User>()
                .Ignore(u => u.Reminders)
                .HasMany<Remind>("_reminders")
                .WithOne()
                .HasForeignKey(r => r.UserId);

        }

    }

}
