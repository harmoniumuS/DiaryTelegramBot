using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class AwaitingDeleteRecordSelection:IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;
    public AwaitingDeleteRecordSelection(ITelegramBotClient botClient,UserContext userContext)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {
        var messages = await _userContext.GetMessagesAsync(user.Id);

        if (!messages.Any())
        {
            await _botClient.SendMessage(chatId, "У вас нет записей для удаления.", cancellationToken: cancellationToken);
            return;
        }
        var formattedRecords = messages
            .Select((record, index) => $"{index + 1}. {record.SentTime:yyyy-MM-dd HH:mm}: {record.Text}")
            .ToList();

        user.CurrentStatus = UserStatus.AwaitingRemoveSelectedRecord;
        await BotKeyboardManager.SendRemoveKeyboardAsync(_botClient, chatId, formattedRecords, cancellationToken);
    }
}