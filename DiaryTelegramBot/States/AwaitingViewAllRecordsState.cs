using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using Telegram.Bot;

namespace DiaryTelegramBot.States;
[TelegramCallbackCommand("view_records")]
public class AwaitingViewAllRecordsState:IState
{
    private readonly UserContext _userContext;
    private ITelegramBotClient _botClient;

    public AwaitingViewAllRecordsState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }
    public async Task Handle(StateContext stateContext,string data = null)
    {
        try
        {
            var messages = await _userContext.GetMessagesAsync(stateContext.User.Id);

            if (messages.Any())
            {
                var groupedMessages = messages
                    .GroupBy(m => m.SentTime.ToString("yyyy-MM-dd HH:mm"))
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {string.Join(", ", g.Select(m => m.Text))}");

                var dataString = string.Join("\n", groupedMessages);

                await _botClient.SendMessage(stateContext.ChatId,dataString,cancellationToken: stateContext.CancellationToken);
            }
            else
            {
                await _botClient.SendMessage(stateContext.ChatId,"Записи не найдены!",cancellationToken: stateContext.CancellationToken);
            }
            stateContext.User.CurrentStatus = UserStatus.None;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении данных для пользователя {stateContext.User.Id}: {ex.Message}");
            await _botClient.SendMessage(stateContext.ChatId,"\"Произошла ошибка при обработке вашего запроса.\"",cancellationToken: stateContext.CancellationToken);
        }
    }
}