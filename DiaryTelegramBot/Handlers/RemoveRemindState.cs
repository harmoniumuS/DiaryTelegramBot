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
    private readonly UserStateHandler _userStateHandler;
    private readonly ITelegramBotClient _botClient;

    public RemoveRemindState(
        UserContext userContext,
        UserStateHandler userStateHandler, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _userStateHandler = userStateHandler;
        _botClient = botClient;
    }

    public async Task Handle(UserStateHandler handler, User user, long chatId, CancellationToken cancellationToken)
    {

        var reminders = await _userContext.GetUserRemindDataAsync(user.Id);

        if (reminders == null || user.SelectedIndex > reminders.Count)
        {
            await _botClient.SendMessage(chatId, "Напоминание с таким номером не найдено.");
            _userStateHandler.SetState(user.Id, UserStatus.None);
            return;
        }

        var reminderToDelete = reminders[user.SelectedIndex - 1];
        var success = await _userContext.DeleteUserRemindDataAsync(user.Id, reminderToDelete.Id);

        if (success)
        {
            await _botClient.SendMessage(chatId, "Напоминание успешно удалено.");
        }
        else
        {
            await _botClient.SendMessage(chatId, "Ошибка при удалении напоминания.");
            _userStateHandler.SetState(user.Id, UserStatus.None);
            return;
        }

        var updatedReminders = await _userContext.GetUserRemindDataAsync(user.Id);

        if (updatedReminders == null || !updatedReminders.Any())
        {
            await _botClient.SendMessage(chatId, "У вас больше нет напоминаний.");
            _userStateHandler.SetState(user.Id, UserStatus.None);
            return;
        }

        var formatted = updatedReminders
            .Select((r, i) => $"{i + 1}. {r.Time:yyyy-MM-dd HH:mm} | {r.Message}")
            .ToList();

        _userStateHandler.SetState(user.Id, UserStatus.None);

        await BotKeyboardManager.SendRemoveKeyboardAsync(
            _botClient,
            chatId,
            formatted,
            cancellationToken,
            iSReminderKeyboard: true
        );
    }
}
