using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

[TelegramCallbackCommand("delete_")]
public class AwaitingRemoveRecordState : IState
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserContext _userContext;

    public AwaitingRemoveRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext, string data = null)
    {
        if (!int.TryParse(data, out int index))
        {
            await _botClient.SendMessage(stateContext.ChatId, 
                "Некорректный формат индекса.", cancellationToken: stateContext.CancellationToken);
            stateContext.User.CurrentStatus = UserStatus.None;
            return;
        }

        var messages = await _userContext.GetMessagesAsync(stateContext.User.Id);

        if (index < 0 || index >= messages.Count)
        {
            await _botClient.SendMessage(stateContext.ChatId, 
                "Некорректный индекс записи.", cancellationToken: stateContext.CancellationToken);
            return;
        }

        var recordToRemove = messages[index];
        await _userContext.RemoveMessageAsync(stateContext.User.Id, recordToRemove.SentTime, recordToRemove.Text);

        await _botClient.SendMessage(stateContext.ChatId, 
            "Запись успешно удалена.", cancellationToken: stateContext.CancellationToken);

        var remaining = await _userContext.GetMessagesAsync(stateContext.User.Id);

        if (remaining.Any())
        {
            var updated = remaining.Select(r => $"{r.SentTime:yyyy-MM-dd HH:mm}: {r.Text}").ToList();

            await BotKeyboardManager.SendRemoveKeyboardAsync(
                _botClient,
                stateContext.ChatId,
                updated,
                stateContext.CancellationToken,
                sendIntroMessage: false);
        }
        else
        {
            await _botClient.SendMessage(stateContext.ChatId, 
                "Больше нет записей для удаления.", cancellationToken: stateContext.CancellationToken);
        }

        stateContext.User.CurrentStatus = UserStatus.None;
    }
}
