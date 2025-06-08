using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

[TelegramCallbackCommand("time_hour_", UserStatus.AwaitingTime)]
[TelegramCallbackCommand("time_minute_", UserStatus.AwaitingTime)]
public class AwaitingTimeState : IState
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserContext _userContext;

    public AwaitingTimeState(ITelegramBotClient botClient, UserContext userContext)
    {
        _botClient = botClient;
        _userContext = userContext;
    }

    public async Task Handle(StateContext stateContext, string data = null)
    {
        var callbackData = stateContext.CallbackData;

        if (!string.IsNullOrEmpty(callbackData))
        {
            if (callbackData.StartsWith("time_"))
            {
                var parts = callbackData.Split('_');
                if (parts.Length == 3)
                {
                    switch (parts[1])
                    {
                        case "hour":
                            await HandleHourSelection(stateContext, parts[2]);
                            return;

                        case "minute":
                            await HandleMinuteSelection(stateContext, parts[2]);
                            return;
                    }
                }
            }
            else if (callbackData == "return_hour_selection")
            {
                await BotKeyboardManager.SendTimeMarkUp(_botClient, stateContext);
                return;
            }
        }

        await _botClient.SendMessage(stateContext.ChatId,
            "Неверный формат. Пожалуйста, выберите время с помощью кнопок.",
            cancellationToken: stateContext.CancellationToken);
    }

    private async Task HandleHourSelection(StateContext stateContext, string hourString)
    {
        if (int.TryParse(hourString, out int hour) && hour is >= 0 and <= 23)
        {
            var minuteKeyboard = BotKeyboardManager.SendMinutesMarkUp(hour);
            await _botClient.EditMessageText(
                stateContext.ChatId,
                stateContext.CallBackQueryId,
                "Вы выбрали час. Теперь выберите минуты:",
                replyMarkup: minuteKeyboard,
                cancellationToken: stateContext.CancellationToken);
        }
        else
        {
            await _botClient.SendMessage(stateContext.ChatId,
                "Некорректный час. Попробуйте ещё раз.",
                cancellationToken: stateContext.CancellationToken);
        }
    }

    private async Task HandleMinuteSelection(StateContext stateContext, string timeValue)
    {
        var timeParts = timeValue.Split(':');
        if (timeParts.Length == 2
            && int.TryParse(timeParts[0], out int hour)
            && int.TryParse(timeParts[1], out int minute)
            && hour is >= 0 and <= 23
            && minute is >= 0 and <= 59)
        {
            var user = stateContext.User;

            if (stateContext.TempRecord != null)
            {
                var date = stateContext.TempRecord.SentTime.Date;
                stateContext.TempRecord.SentTime = date.AddHours(hour).AddMinutes(minute);
                
                user.CurrentStatus = UserStatus.AwaitingAddRecord;
                await _userContext.UpdateUserAsync(user);
                
                await _botClient.EditMessageText(
                    stateContext.ChatId,
                    stateContext.CallBackQueryId,
                    $"Запись сохранена на дату и время: {stateContext.TempRecord.SentTime:dd.MM.yyyy HH:mm}.",
                    cancellationToken: stateContext.CancellationToken);
            }
            else
            {
                await _botClient.SendMessage(stateContext.ChatId,
                    "Сначала выберите дату. Начните заново.",
                    cancellationToken: stateContext.CancellationToken);
            }
        }
        else
        {
            await _botClient.SendMessage(stateContext.ChatId,
                "Некорректный формат минут. Попробуйте ещё раз.",
                cancellationToken: stateContext.CancellationToken);
        }
    }
}
