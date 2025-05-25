using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class RemoveRecordState:IState
{
    private ITelegramBotClient _botClient;
    private readonly UserContext _userContext;
    private readonly UserStateHandler _userStateHandler;

    public RemoveRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }
    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {
        var messages = await _userContext.GetMessagesAsync(user.Id);
    
        if (messages.Count == 0)
        {
            await _botClient.SendMessage(chatId, "У вас нет записей для удаления.", cancellationToken: cancellationToken);
            return;
        }
    
        if (user.TempRecord.SelectedIndex < 0 || user.TempRecord.SelectedIndex >= messages.Count)
        {
            await _botClient.SendMessage(chatId, "Некорректный выбор записи.", cancellationToken: cancellationToken);
            return;
        }

        var recordToRemove = messages[user.TempRecord.SelectedIndex];
        await _userContext.RemoveMessageAsync(user.Id, recordToRemove.SentTime, recordToRemove.Text);
    
        await _botClient.SendMessage(chatId, "Запись успешно удалена.", cancellationToken: cancellationToken);

        var remaining = await _userContext.GetMessagesAsync(user.Id);

        if (remaining.Any())
        {
            var updated = remaining.Select(r => $"{r.SentTime:yyyy-MM-dd HH:mm}: {r.Text}").ToList();
            await BotKeyboardManager.SendRemoveKeyboardAsync(_botClient, chatId, updated, cancellationToken, sendIntroMessage: false);
        }
        else
        {
            await _botClient.SendMessage(chatId, "Больше нет записей для удаления.", cancellationToken: cancellationToken);
        }

        user.CurrentStatus = UserStatus.None;
    }

    
    
}