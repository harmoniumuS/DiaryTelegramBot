using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class RemoveRemindState : IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;

    public RemoveRemindState(
        UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {

        var reminders = await _userContext.GetUserRemindDataAsync(user.Id);

        if (reminders == null || user.TempRemind.SelectedIndex > reminders.Count)
        {
            await _botClient.SendMessage(chatId, "Напоминание с таким номером не найдено.");
            return;
        }

        var reminderToDelete = reminders[user.TempRemind.SelectedIndex - 1];
        var success = await _userContext.DeleteUserRemindDataAsync(user.Id, reminderToDelete.Id);

        if (success)
        {
            await _botClient.SendMessage(chatId, "Напоминание успешно удалено.");
        }
        else
        {
            await _botClient.SendMessage(chatId, "Ошибка при удалении напоминания.");
            return;
        }

        var updatedReminders = await _userContext.GetUserRemindDataAsync(user.Id);

        if (updatedReminders == null || !updatedReminders.Any())
        {
            await _botClient.SendMessage(chatId, "У вас больше нет напоминаний.");
            return;
        }

        var formatted = updatedReminders
            .Select((r, i) => $"{i + 1}. {r.Time:yyyy-MM-dd HH:mm} | {r.Record}")
            .ToList();

        await BotKeyboardManager.SendRemoveKeyboardAsync(
            _botClient,
            chatId,
            formatted,
            cancellationToken,
            iSReminderKeyboard: true
        );
    }
}
