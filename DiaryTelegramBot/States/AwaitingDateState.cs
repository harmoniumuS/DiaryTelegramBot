using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

[TelegramCallbackCommand("date")]
[TelegramCallbackCommand("calendar:prev")]
[TelegramCallbackCommand("calendar:next")]
public class AwaitingDateState : IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingDateState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        if (stateContext.CallbackData.StartsWith("date:"))
        {
            var datePart = stateContext.CallbackData.Substring("date:".Length);
            if (DateTime.TryParse(datePart, out var parsedDate))
            {
                if (stateContext.User.CurrentStatus == UserStatus.AwaitingDate)
                {
                    if (stateContext.User.TempRecord != null)
                        stateContext.User.TempRecord.SentTime = parsedDate;
                    
                    await _botClient.SendMessage(
                        stateContext.ChatId,
                        $"Вы выбрали дату: {parsedDate:dd.MM.yyyy}.",
                        cancellationToken: stateContext.CancellationToken);
                    await BotKeyboardManager.SendTimeMarkUp(_botClient,stateContext.ChatId,stateContext.CancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(
                        stateContext.ChatId,
                        "Неверное состояние. Попробуйте ещё раз.",
                        cancellationToken: stateContext.CancellationToken);
                }
            }
            else
            {
                await _botClient.SendMessage(
                    stateContext.ChatId,
                    "Некорректная дата, попробуйте ещё раз.",
                    cancellationToken: stateContext.CancellationToken);
            }
        }
        else if (stateContext.CallbackData.StartsWith("calendar:prev:") || stateContext.CallbackData.StartsWith("calendar:next:"))
        {
            var parts = stateContext.CallbackData.Split(':');
            var action = parts[1];
            var partOfDate = stateContext.CallbackData.Substring($"calendar:{action}:".Length);

            if (DateTime.TryParse(partOfDate, out var changeMonthDate))
            {
                var newDate = action == "prev"
                    ? changeMonthDate.AddMonths(-1)
                    : changeMonthDate.AddMonths(1);

               await BotKeyboardManager.SendDataKeyboardAsync(_botClient,stateContext.ChatId,stateContext.CancellationToken,newDate);
            }
            else
            {
                await _botClient.SendMessage(
                    stateContext.ChatId,
                    "Некорректная дата для перехода, попробуйте ещё раз.",
                    cancellationToken: stateContext.CancellationToken);
            }
        }
        else
        {
            await _botClient.SendMessage(
                stateContext.ChatId,
                "Пожалуйста, выберите дату с помощью календаря.",
                cancellationToken: stateContext.CancellationToken);
        }
    }
}
