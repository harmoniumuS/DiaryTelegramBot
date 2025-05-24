using Microsoft.EntityFrameworkCore;
using ReminderWorker.Data;
using Telegram.Bot.Types;
using User = DiaryTelegramBot.Models.User;

namespace DiaryTelegramBot.Data
{
    public class UserContext
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UserContext(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<User> GetUserAsync(long userId)
        {
            var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = await CreateUserAsync(context,userId);
            }
            return user;
        }

        private async Task<User> CreateUserAsync(AppDbContext context,long userId)
        {
            var user = new User { Id = userId };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }
        public async Task<List<Record>> GetMessagesAsync(long userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return await context.Messages
                .Where(m => m.UserId == user.Id)
                .ToListAsync();
        }
         public async Task AddMessageAsync(User user, string textMessage, DateTime? createdAt = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = new Record
            {
                UserId = user.Id,
                Text = textMessage,
                SentTime = createdAt ?? DateTime.UtcNow
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();
        }
        public async Task<List<Record>> GetAllMessagesAsync(long userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new List<Record>();

            return await context.Messages
                .Where(m => m.UserId == user.Id)
                .OrderBy(m => m.SentTime)
                .ToListAsync();
        }
        
        public async Task RemoveMessageAsync(long userId, DateTime date, string content)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            var message = await context.Messages
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.SentTime.Date == date.Date && m.Text == content);

            if (message != null)
            {
                context.Messages.Remove(message);
                await context.SaveChangesAsync();
            }
        }
        public async Task SaveRemindDataAsync(Remind remind)
        {
            using var scope = _scopeFactory.CreateScope();
    
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remindContext = scope.ServiceProvider.GetRequiredService<RemindContext>();

            var userExists = await appDbContext.Users.AnyAsync(u => u.Id == remind.UserId);
            if (!userExists)
            {
                throw new KeyNotFoundException("Пользователь не найден");
            }
            remindContext.Reminds.Add(remind);
            await remindContext.SaveChangesAsync();
        }

    }

}
