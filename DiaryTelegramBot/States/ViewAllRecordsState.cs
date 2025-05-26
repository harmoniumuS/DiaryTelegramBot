using DiaryTelegramBot.Data;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class ViewAllRecordsState:IState
{
    private readonly UserContext _userContext;
    private ITelegramBotClient _botClient;

    public ViewAllRecordsState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }
    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {
        try
        {
            var messages = await _userContext.GetMessagesAsync(user.Id);

            if (messages.Any())
            {
                var groupedMessages = messages
                    .GroupBy(m => m.SentTime.ToString("yyyy-MM-dd HH:mm"))
                    .OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}: {string.Join(", ", g.Select(m => m.Text))}");

                var dataString = string.Join("\n", groupedMessages);

                await _botClient.SendMessage(chatId,dataString,cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendMessage(chatId,"Записи не найдены!",cancellationToken: cancellationToken);
            }
            user.CurrentStatus = UserStatus.None;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении данных для пользователя {user.Id}: {ex.Message}");
            await _botClient.SendMessage(chatId,"\"Произошла ошибка при обработке вашего запроса.\"",cancellationToken: cancellationToken);
        }
    }
}