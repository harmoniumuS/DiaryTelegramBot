using DiaryTelegramBot.ReminderWorker.Data;
using Microsoft.EntityFrameworkCore;

namespace DiaryTelegramBot.Data;

public class RemindContext(DbContextOptions<RemindContext> options):DbContext(options)
{
    public DbSet<Remind> Reminds { get; set; }

    public async Task<IReadOnlyList<Remind>> ReadRemindsInTimespan(DateTime from, DateTime to)=>
        await Reminds.Where(r =>
            !r.IsRemind
            && r.Time >= from
            && r.Time <= to).ToListAsync();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Remind>().HasKey(r=>r.Id);
    }
}