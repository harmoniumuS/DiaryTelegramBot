using DiaryTelegramBot.Data;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class ViewAllRemindersState:IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;

    public ViewAllRemindersState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {
        try
        {
            if (user.Reminders.Any())
            {
                var message = string.Join("\n", user.Reminders.Select(r =>
                    $"{r.Time:yyyy-MM-dd HH:mm} — {r.Record}"
                ));

                await _botClient.SendMessage(chatId, message);
            }
            else 
            {
                await _botClient.SendMessage(chatId, "У вас нет активных напоминаний.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении напоминаний пользователя {user.Id}: {ex.Message}");
            await _botClient.SendMessage(chatId, "Произошла ошибка при получении напоминаний.");
        }
    }
}