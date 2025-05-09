using DiaryTelegramBot.Data;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class ViewAllRecordsHandler
{
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserDataService _userDataService;

    public ViewAllRecordsHandler(BotClientWrapper botClientWrapper, UserDataService userDataService)
    {
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
    }
    public async Task HandleViewRecords(ITelegramBotClient botClient, long chatId, string userId,CancellationToken cancellationToken)
    {
        try
        {
            var userData = await _userDataService.GetUserDataAsync(userId);
            if (userData.Any())
            {
                var dataString = string.Join("\n", userData.Select(r => $"{r.Key.ToString("yyyy-MM-dd")}: {string.Join(", ", r.Value)}"));
                await _botClientWrapper.SendTextMessageAsync(chatId, dataString,cancellationToken);
            }
            else
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Записи не найдены!",cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении данных для пользователя {userId}: {ex.Message}");
            await _botClientWrapper.SendTextMessageAsync(chatId, "Произошла ошибка при обработке вашего запроса.",cancellationToken);
        }
    }
}