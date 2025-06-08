using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

[TelegramCallbackCommand("remove_record")]
public class AwaitingDeleteRecordSelection:IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;
    public AwaitingDeleteRecordSelection(ITelegramBotClient botClient,UserContext userContext)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext, string data = null)
    {
        try
        {
            var messages = await _userContext.GetMessagesAsync(stateContext.User.Id);

            if (!messages.Any())
            {
                await _botClient.SendMessage(
                    stateContext.ChatId,
                    "У вас нет записей для удаления.",
                    cancellationToken: stateContext.CancellationToken);
                return;
            }

            var formattedRecords = messages
                .Select((record, index) => $"{index + 1}. {record.SentTime:yyyy-MM-dd HH:mm}: {record.Text}")
                .ToList();

            stateContext.User.CurrentStatus = UserStatus.AwaitingRemoveSelectedRecord;

            await BotKeyboardManager.SendRemoveKeyboardAsync(_botClient, formattedRecords, stateContext);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка в AwaitingDeleteRecordSelection.Handle: {ex}");

            try
            {
                if (stateContext.ChatId != 0)
                {
                    await _botClient.SendMessage(
                        stateContext.ChatId,
                        "Произошла ошибка при получении записей. Попробуйте позже.",
                        cancellationToken: stateContext.CancellationToken);
                }
            }
            catch (Exception sendEx)
            {
                Console.Error.WriteLine($"Ошибка при отправке сообщения об ошибке: {sendEx}");
            }
        }
    }


}