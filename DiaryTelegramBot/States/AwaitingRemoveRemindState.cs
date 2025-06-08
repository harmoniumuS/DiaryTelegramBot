using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class AwaitingRemoveRemindState:IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;

    public AwaitingRemoveRemindState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        var messages = await _userContext.GetMessagesAsync(stateContext.User.Id);

        if (!messages.Any())
        {
            await _botClient.SendMessage(stateContext.ChatId, "У вас нет записей для удаления.", cancellationToken: stateContext.CancellationToken);
            return;
        }
        var formattedRecords = messages
            .Select((record, index) => $"{index + 1}. {record.SentTime:yyyy-MM-dd HH:mm}: {record.Text}")
            .ToList();
        
        
        await BotKeyboardManager.SendRemoveKeyboardAsync(_botClient,formattedRecords, stateContext);
    }
}