using DiaryTelegramBot.Data;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

public class AwaitingViewAllRemindersState:IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;

    public AwaitingViewAllRemindersState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        try
        {
            if (stateContext.User.Reminders.Any())
            {
                var message = string.Join("\n", stateContext.User.Reminders.Select(r =>
                    $"{r.Time:yyyy-MM-dd HH:mm} — {r.Record}"
                ));

                await _botClient.SendMessage(stateContext.ChatId, message);
            }
            else 
            {
                await _botClient.SendMessage(stateContext.ChatId, "У вас нет активных напоминаний.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении напоминаний пользователя {stateContext.User.Id}: {ex.Message}");
            await _botClient.SendMessage(stateContext.ChatId, "Произошла ошибка при получении напоминаний.");
        }
    }
}