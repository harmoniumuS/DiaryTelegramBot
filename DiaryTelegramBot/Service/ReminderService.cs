using DiaryTelegramBot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace DiaryTelegramBot.Service;
//почитать и понять как тут все работает
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
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.Now;

            var reminders = await context.UserReminders
                .Where(r => !r.IsRemind &&
                            r.ReminderTime <= now.AddMinutes(5) &&
                            r.ReminderTime > now)
                .Include(r => r.User)
                .ToListAsync(stoppingToken);

            foreach (var remind in reminders)
            {
                var telegramUserId = remind.User.UserId;
                var message = remind.ReminderMessage;
                try
                {
                    await SendTelegramMessageAsync(telegramUserId, $"Напоминание: {message}");
                    remind.IsRemind = true;
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
    private Task SendTelegramMessageAsync(string chatId, string message)
    {
       return _botClient.SendMessage(chatId, message);
    }
}