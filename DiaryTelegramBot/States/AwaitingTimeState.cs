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
                try
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в обработке времени: {ex.Message}");
                    // Можно, например, не отправлять ошибку пользователю,
                    // или отправить простое сообщение
                    try
                    {
                        await _botClient.SendMessage(stateContext.ChatId, "Произошла ошибка, попробуйте снова.");
                    }
                    catch { }
                }
            }
        }
        else if (callbackData == "return_hour_selection")
        {
            try
            {
                await BotKeyboardManager.SendTimeMarkUp(_botClient, stateContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке TimeMarkUp: {ex.Message}");
            }
            return;
        }
    }

    try
    {
        await _botClient.SendMessage(stateContext.ChatId,
            "Неверный формат. Пожалуйста, выберите время с помощью кнопок.",
            cancellationToken: stateContext.CancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
    }
}

private async Task HandleHourSelection(StateContext stateContext, string hourString)
{
    if (int.TryParse(hourString, out int hour) && hour is >= 0 and <= 23)
    {
        var minuteKeyboard = BotKeyboardManager.SendMinutesMarkUp(hour);
        try
        {
            await _botClient.EditMessageText(
                stateContext.ChatId,
                stateContext.CallBackQueryId,
                "Вы выбрали час. Теперь выберите минуты:",
                replyMarkup: minuteKeyboard,
                cancellationToken: stateContext.CancellationToken);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
        {
            // Игнорируем
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при редактировании сообщения: {ex.Message}");
        }
    }
    else
    {
        try
        {
            await _botClient.SendMessage(stateContext.ChatId,
                "Некорректный час. Попробуйте ещё раз.",
                cancellationToken: stateContext.CancellationToken);
        }
        catch { }
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
        }
        else
        {
            try
            {
                await _botClient.SendMessage(stateContext.ChatId,
                    "Сначала выберите дату. Начните заново.",
                    cancellationToken: stateContext.CancellationToken);
            }
            catch { }
        }
    }
    else
    {
        try
        {
            await _botClient.SendMessage(stateContext.ChatId,
                "Некорректный формат минут. Попробуйте ещё раз.",
                cancellationToken: stateContext.CancellationToken);
        }
        catch { }
    }
}
}
