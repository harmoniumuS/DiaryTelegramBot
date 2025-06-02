using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using ReminderWorker.Data;
using Telegram.Bot;


namespace DiaryTelegramBot.Handlers;

public class AwaitingAddRemindState : IState
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserContext _userContext;

    public AwaitingAddRemindState(ITelegramBotClient botClient, UserContext userContext)
    {
        _botClient = botClient;
        _userContext = userContext;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        var userData = await _userContext.GetMessagesAsync(stateContext.User.Id);
        var allRecords = userData
            .Select(m => $"{m.SentTime:yyyy-MM-dd HH:mm} {m.Text}")
            .ToList();

        if (allRecords.Count == 0)
        {
            await _botClient.SendMessage(stateContext.ChatId, "Нет доступных записей для установки напоминания.", cancellationToken:stateContext.CancellationToken);
            return;
        }

        stateContext.User.CurrentStatus = UserStatus.AwaitingRemind;

        await BotKeyboardManager.SendAddRemindersKeyboard(_botClient, stateContext.ChatId, allRecords, cancellationToken:stateContext.CancellationToken);
    }

    public async Task HandleAddRemind(User user, long chatId, int index, CancellationToken cancellationToken)
    {
        if (user.CurrentStatus != UserStatus.AwaitingRemind || user.Messages == null || index < 0 || index >= user.Messages.Count)
        {
            await _botClient.SendMessage(chatId, "Некорректный выбор записи.", cancellationToken: cancellationToken);
            return;
        }

        var selectedRecord = user.Messages[index];
        var recordDateTimeString = selectedRecord.Text.Substring(0, 16); 

        if (DateTime.TryParseExact(recordDateTimeString, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var selectedDateTime))
        {
            user.TempRecord.SentTime = selectedDateTime;
            user.TempRecord.Text = selectedRecord.Text.Substring(17); 

            await _botClient.SendMessage(
                chatId,
                $"Вы выбрали: {selectedRecord}\nТеперь выберите смещение времени.",
                replyMarkup: BotKeyboardManager.GetReminderKeyboard(),
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.SendMessage(chatId, "Ошибка при извлечении времени из записи.", cancellationToken: cancellationToken);
        }
    }

    public async Task HandleRemindOffset(User user, long chatId, int offsetMinutes, CancellationToken cancellationToken)
    {
        var record = user.TempRecord;

        if (record.SentTime == default || string.IsNullOrWhiteSpace(record.Text))
        {
            await _botClient.SendMessage(chatId, "Ошибка: не выбрана корректная запись.", cancellationToken: cancellationToken);
            return;
        }

        var remindTime = record.SentTime.AddMinutes(offsetMinutes);

        var remind = new Remind
        {
            UserId = user.Id,
            Time = remindTime,
            Record = record.Text,
            IsRemind = false
        };

        await _userContext.SaveRemindDataAsync(remind);

        string message = offsetMinutes switch
        {
            -5 => $"Напоминание установлено на {remind.Time:dd.MM.yyyy HH:mm}, я напомню за 5 минут до события.",
            -30 => $"Напоминание установлено на {remind.Time:dd.MM.yyyy HH:mm}, я напомню за 30 минут до события.",
            -60 => $"Напоминание установлено на {remind.Time:dd.MM.yyyy HH:mm}, я напомню за час до события.",
            -1440 => $"Напоминание установлено на {remind.Time:dd.MM.yyyy HH:mm}, я напомню за день до события.",
            _ => $"Напоминание установлено на {remind.Time:dd.MM.yyyy HH:mm}."
        };

        await _botClient.SendMessage(chatId, message, cancellationToken: cancellationToken);
        user.CurrentStatus = UserStatus.None;
        user.TempRecord = new(); 
    }
}
