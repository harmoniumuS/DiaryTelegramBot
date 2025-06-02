using System.Globalization;
using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;    
[TelegramCallbackCommand("time_hour_", UserStatus.AwaitingTime)]
[TelegramCallbackCommand("time_minute_", UserStatus.AwaitingTime)]
public class AwaitingTimeState : IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingTimeState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext, string data = null)
    {
        if (!string.IsNullOrEmpty(stateContext.CallbackData) && stateContext.CallbackData.StartsWith("time_"))
        {
            var parts = stateContext.CallbackData.Split('_');

            if (parts.Length == 3)
            {
                var timeType = parts[1]; 
                var valuePart = parts[2];

                if (timeType == "hour")
                {
                    if (int.TryParse(valuePart, out int hour) && hour >= 0 && hour <= 23)
                    {
                        var minuteKeyboard = BotKeyboardManager.SendMinutesMarkUp(hour);
                        await _botClient.SendMessage(stateContext.ChatId,
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

                    return;
                }
                else if (timeType == "minute")
                {
                    var timeParts = valuePart.Split(':');
                    if (timeParts.Length == 2
                        && int.TryParse(timeParts[0], out int hour)
                        && int.TryParse(timeParts[1], out int minute)
                        && hour >= 0 && hour <= 23
                        && minute >= 0 && minute <= 59)
                    {
                        if (stateContext.User.TempRecord != null)
                        {
                            var date = stateContext.User.TempRecord.SentTime.Date;
                            stateContext.User.TempRecord.SentTime = date.AddHours(hour).AddMinutes(minute);
                            stateContext.User.CurrentStatus = UserStatus.AwaitingAddRecord;

                            await _botClient.SendMessage(stateContext.ChatId,
                                $"Время установлено: {stateContext.User.TempRecord.SentTime:HH:mm}.",
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

                    return;
                }
            }
        }

        if (stateContext.CallbackData == "return_hour_selection")
        {
            await BotKeyboardManager.SendTimeMarkUp(_botClient, stateContext.ChatId, stateContext.CancellationToken);
            return;
        }

        await _botClient.SendMessage(stateContext.ChatId,
            "Неверный формат. Пожалуйста, выберите время с помощью кнопок.",
            cancellationToken: stateContext.CancellationToken);
    }
}
