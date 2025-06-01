using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

public class AwaitingDateState : IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingDateState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken, string dataHandler = null)
    {
        if (dataHandler.StartsWith("date:"))
        {
            var datePart = dataHandler.Substring("date:".Length);
            if (DateTime.TryParse(datePart, out var parsedDate))
            {
                if (user.CurrentStatus == UserStatus.AwaitingDate)
                {
                    if (user.TempRecord != null)
                        user.TempRecord.SentTime = parsedDate;

                    user.CurrentStatus = UserStatus.AwaitingTime; 

                    await _botClient.SendMessage(
                        chatId,
                        $"Вы выбрали дату: {parsedDate:dd.MM.yyyy}.",
                        cancellationToken: cancellationToken);
                    await BotKeyboardManager.SendTimeMarkUp(_botClient,chatId,cancellationToken);
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
            var parts = dataHandler.Split(':');
            var action = parts[1];
            var partOfDate = dataHandler.Substring($"calendar:{action}:".Length);

            if (DateTime.TryParse(partOfDate, out var changeMonthDate))
            {
                var newDate = action == "prev"
                    ? changeMonthDate.AddMonths(-1)
                    : changeMonthDate.AddMonths(1);

               await BotKeyboardManager.SendDataKeyboardAsync(_botClient,chatId,cancellationToken,newDate);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId,
                    "Некорректная дата для перехода, попробуйте ещё раз.",
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                "Пожалуйста, выберите дату с помощью календаря.",
                cancellationToken: cancellationToken);
        }
    }
}
