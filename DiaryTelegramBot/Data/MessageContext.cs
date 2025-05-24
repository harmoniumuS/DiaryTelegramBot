using DiaryTelegramBot.Models;
using Microsoft.EntityFrameworkCore;

namespace DiaryTelegramBot.Data;

public class MessageContext
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MessageContext(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
     public async Task AddMessageAsync(User user, string textMessage, DateTime? createdAt = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var message = new Message
            {
                UserId = user.Id,
                Text = textMessage,
                SentTime = createdAt ?? DateTime.UtcNow
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();
        }

        public async Task<List<Message>> GetMessagesAsync(long userId)
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
        public async Task<List<Message>> GetAllMessagesAsync(long userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new List<Message>();

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
}