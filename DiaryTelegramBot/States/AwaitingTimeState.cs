using System.Globalization;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class AwaitingTimeState : IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingTimeState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken, string dataHandler = null)
    {
        if (dataHandler.StartsWith("time_"))
        {
            var parts = dataHandler.Split('_');

            if (parts.Length == 2)
            {
                if (TimeSpan.TryParse(parts[1], out var time) && time.Hours >= 0 && time.Hours <= 23)
                {
                    
                    var minuteKeyboard = BotKeyboardManager.SendMinutesMarkUp(time.Hours);
                    await _botClient.SendMessage(chatId,
                        $"Вы выбрали час. Теперь выберите минуты:",
                        replyMarkup: minuteKeyboard,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Некорректный час. Попробуйте ещё раз.", cancellationToken: cancellationToken);
                }

                return;
            }

            if (parts.Length == 3)
            {
                if (int.TryParse(parts[1], out var hour) && int.TryParse(parts[2], out var minute))
                {
                    if (user.TempRecord?.SentTime != null)
                    {
                        var date = user.TempRecord.SentTime.Date;
                        user.TempRecord.SentTime = date.AddHours(hour).AddMinutes(minute);
                        user.CurrentStatus = UserStatus.AwaitingAddRecord;

                        await _botClient.SendMessage(chatId,
                            $"Время установлено: {user.TempRecord.SentTime:HH:mm}.",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, "Сначала выберите дату. Начните заново.", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Некорректный формат минут. Попробуйте ещё раз.", cancellationToken: cancellationToken);
                }

                return;
            }
        }

        if (dataHandler == "return_hour_selection")
        {
            await BotKeyboardManager.SendTimeMarkUp(_botClient, chatId, cancellationToken);
            return;
        }

        await _botClient.SendMessage(chatId, "Неверный формат. Пожалуйста, выберите время с помощью кнопок.", cancellationToken: cancellationToken);
    }
}
