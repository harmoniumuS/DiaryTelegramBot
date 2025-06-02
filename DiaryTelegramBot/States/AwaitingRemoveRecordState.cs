using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

[TelegramCallbackCommand("delete_")]

public class AwaitingRemoveRecordState:IState
{
    private ITelegramBotClient _botClient;
    private readonly UserContext _userContext;
    private readonly UserStateHandler _userStateHandler;

    public AwaitingRemoveRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }
    public async Task Handle(StateContext stateContext,string data = null)
    {
        var messages = await _userContext.GetMessagesAsync(stateContext.User.Id);
    
        if (messages.Count == 0)
        {
            await _botClient.SendMessage(stateContext.ChatId, "У вас нет записей для удаления.", cancellationToken: stateContext.CancellationToken);
            return;
        }
    
        if (stateContext.User.TempRecord.SelectedIndex < 0 || stateContext.User.TempRecord.SelectedIndex >= messages.Count)
        {
            await _botClient.SendMessage(stateContext.ChatId, "Некорректный выбор записи.", cancellationToken: stateContext.CancellationToken);
            return;
        }

        var recordToRemove = messages[stateContext.User.TempRecord.SelectedIndex];
        await _userContext.RemoveMessageAsync(stateContext.User.Id, recordToRemove.SentTime, recordToRemove.Text);
    
        await _botClient.SendMessage(stateContext.ChatId, "Запись успешно удалена.", cancellationToken: stateContext.CancellationToken);

        var remaining = await _userContext.GetMessagesAsync(stateContext.User.Id);

        if (remaining.Any())
        {
            var updated = remaining.Select(r => $"{r.SentTime:yyyy-MM-dd HH:mm}: {r.Text}").ToList();
            await BotKeyboardManager.SendRemoveKeyboardAsync(_botClient, stateContext.ChatId, updated, stateContext.CancellationToken, sendIntroMessage: false);
        }
        else
        {
            await _botClient.SendMessage(stateContext.ChatId, "Больше нет записей для удаления.", cancellationToken: stateContext.CancellationToken);
        }

        stateContext.User.CurrentStatus = UserStatus.None;
    }

    
    
}