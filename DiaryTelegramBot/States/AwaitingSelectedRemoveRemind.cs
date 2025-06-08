using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

public class AwaitingSelectedRemoveRemind : IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;

    public AwaitingSelectedRemoveRemind(
        UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {

        var reminders = await _userContext.GetUserRemindDataAsync(stateContext.User.Id);

        if (stateContext.TempRemind != null && stateContext.TempRemind.SelectedIndex > reminders.Count)
        {
            await _botClient.SendMessage(stateContext.ChatId, "Напоминание с таким номером не найдено.");
            return;
        }

        if (stateContext.TempRemind != null)
        {
            var reminderToDelete = reminders[stateContext.TempRemind.SelectedIndex - 1];
            var success = await _userContext.DeleteUserRemindDataAsync(stateContext.User.Id, reminderToDelete.Id);

            if (success)
            {
                await _botClient.SendMessage(stateContext.ChatId, "Напоминание успешно удалено.");
            }
            else
            {
                await _botClient.SendMessage(stateContext.ChatId, "Ошибка при удалении напоминания.");
                return;
            }
        }

        var updatedReminders = await _userContext.GetUserRemindDataAsync(stateContext.User.Id);

        if (!updatedReminders.Any())
        {
            await _botClient.SendMessage(stateContext.ChatId, "У вас больше нет напоминаний.");
            return;
        }

        var formatted = updatedReminders
            .Select((r, i) => $"{i + 1}. {r.Time:yyyy-MM-dd HH:mm} | {r.Record}")
            .ToList();

        await BotKeyboardManager.SendRemoveKeyboardAsync(
            _botClient,
            stateContext.ChatId,
            formatted,
            stateContext.CancellationToken,
            iSReminderKeyboard: true
        );
    }
}
