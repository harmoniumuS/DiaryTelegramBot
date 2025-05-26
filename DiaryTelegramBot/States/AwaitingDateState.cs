using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class AwaitingDateState:IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingDateState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {
        var datePart = dataHandler.Substring("date:".Length);
        if (dataHandler.StartsWith("date:"))
        {
            if (DateTime.TryParse(datePart, out var parsedDate))
            {
                if (user.CurrentStatus == UserStatus.AwaitingDate)
                {
                    if (user.TempRecord != null) 
                        user.TempRecord.SentTime = parsedDate;
                    user.CurrentStatus = UserStatus.AwaitingTime;
                                    
                    await _botClient.SendMessage(
                        chatId,
                        $"Вы выбрали дату: {parsedDate:dd.MM.yyyy}. Теперь введите время (в формате ЧЧ:ММ):",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Неверное состояние. Попробуйте ещё раз.",
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    "Некорректная дата, попробуйте ещё раз.",
                    cancellationToken: cancellationToken);
            }
        }
        else if (dataHandler.StartsWith("calendar:prev:") || dataHandler.StartsWith("calendar:next:"))
        {
            var action = dataHandler.Split(':')[0] == "calendar" ? dataHandler.Split(':')[1] : string.Empty;
            var partOfDate = dataHandler.Substring($"calendar:{action}:".Length);
            if (DateTime.TryParse(partOfDate, out var changeMonthDate))
            {
                var newDate = action == "prev" 
                    ? changeMonthDate.AddMonths(-1)
                    : changeMonthDate.AddMonths(1); 
                var calendarMarkup = BotKeyboardManager.CreateCalendarMarkUp(newDate);
                await _botClient.SendMessage(
                    chatId,
                    $"Вы перешли к {newDate:MMMM yyyy}.",
                    replyMarkup: calendarMarkup,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    "Некорректная дата для перехода, попробуйте ещё раз.",
                    cancellationToken: cancellationToken);
            }
        }
    }
}