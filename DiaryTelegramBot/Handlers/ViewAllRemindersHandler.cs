using DiaryTelegramBot.Data;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class ViewAllRemindersHandler
{
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserContext _userContext;

    public ViewAllRemindersHandler(BotClientWrapper botClientWrapper, UserContext userContext)
    {
        _botClientWrapper = botClientWrapper;
        _userContext = userContext;
    }
    public async Task HandleViewReminders(ITelegramBotClient botClient, long chatId, long userId, CancellationToken cancellationToken)
    {
        try
        {
            var reminders = await _userContext.GetUserRemindDataAync(userId);

            if (reminders.Any())
            {
                var message = string.Join("\n", reminders.Select(r =>
                    $"{r.Time:yyyy-MM-dd HH:mm} — {r.Message}"
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