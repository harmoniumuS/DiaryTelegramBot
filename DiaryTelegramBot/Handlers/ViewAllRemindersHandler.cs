using DiaryTelegramBot.Data;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class ViewAllRemindersHandler
{
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserDataService _userDataService;

    public ViewAllRemindersHandler(BotClientWrapper botClientWrapper, UserDataService userDataService)
    {
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
    }
    public async Task HandleViewReminders(ITelegramBotClient botClient, long chatId, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var reminders = await _userDataService.GetUserRemindDataAync(userId);

            if (reminders.Any())
            {
                var message = string.Join("\n", reminders.Select(r =>
                    $"{r.ReminderTime:yyyy-MM-dd HH:mm} — {r.ReminderMessage}"
                ));

                await _botClientWrapper.SendTextMessageAsync(chatId, message, cancellationToken);
            }
            else
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "У вас нет активных напоминаний.", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении напоминаний пользователя {userId}: {ex.Message}");
            await _botClientWrapper.SendTextMessageAsync(chatId, "Произошла ошибка при получении напоминаний.", cancellationToken);
        }
    }
}