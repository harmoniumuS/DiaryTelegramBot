using Microsoft.EntityFrameworkCore;


namespace DiaryTelegramBot.Data
{
    public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserReminder> UserReminders { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(user => user.Id);
        modelBuilder.Entity<UserReminder>().HasKey(r => r.Id);
        modelBuilder.Entity<UserReminder>().Property(r => r.UserId).IsRequired();
        modelBuilder.Entity<UserReminder>().Property(r => r.ReminderTime).IsRequired();
        modelBuilder.Entity<UserReminder>().Property(r => r.IsRemind).HasDefaultValue(true);
    }
}
}
