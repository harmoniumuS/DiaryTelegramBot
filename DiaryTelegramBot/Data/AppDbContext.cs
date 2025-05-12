using Microsoft.EntityFrameworkCore;


namespace DiaryTelegramBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserReminder> UserReminders { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserReminder>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(ur => ur.UserId);
        }
    }

}
