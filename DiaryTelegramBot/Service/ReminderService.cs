using DiaryTelegramBot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace DiaryTelegramBot.Service;
public class ReminderService:BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;
    public ReminderService(IServiceProvider serviceProvider,ITelegramBotClient botClient)
    {
        _botClient = botClient;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("ReminderService: ExecuteAsync стартовал");
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.Now;

            var reminders = await context.UserReminders
                .Where(r => !r.IsRemind &&
                            r.ReminderTime <= now.AddMinutes(1) 
                            && r.ReminderTime >= now.AddSeconds(-30))
                .Include(r => r.User)
                .ToListAsync(stoppingToken);

            foreach (var remind in reminders)
            {
                var telegramUserId = remind.User.UserId;
                var message = remind.ReminderMessage;
                try
                {
                    await _botClient.SendMessage(telegramUserId, $"Напоминаю об: {message}");
                    context.UserReminders.Remove(remind);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке напоминания: {ex.Message}");
                }
            }

            await context.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
