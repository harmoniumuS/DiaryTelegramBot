using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.States;

[TelegramCallbackCommand("add_record",UserStatus.AwaitingContent)]
public class AwaitingContentState:IState
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserContext _userContext;
    public AwaitingContentState(ITelegramBotClient botClient,UserContext userContext)
    {
        _botClient = botClient;
        _userContext = userContext;
    }

    public async Task Handle(StateContext stateContext, string data = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(stateContext.MessageText))
            {
                if (stateContext.TempRecord == null)
                {
                    stateContext.TempRecord = new Record();
                }

                stateContext.TempRecord.Text = stateContext.MessageText;
                stateContext.User.CurrentStatus = UserStatus.AwaitingDate;

                stateContext.MessageText = null;

                await _userContext.UpdateUserAsync(stateContext.User);

                await BotKeyboardManager.SendDataKeyboardAsync(
                    _botClient,
                    stateContext.ChatId,
                    stateContext.CallBackQueryId,
                    stateContext.CancellationToken,
                    DateTime.Now);

                return;
            }

            var replyMarkup = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"));

            try
            {
                await _botClient.EditMessageText(
                    chatId: stateContext.ChatId,
                    messageId: stateContext.CallBackQueryId,
                    text: "Введите текст записи:",
                    replyMarkup: replyMarkup,
                    cancellationToken: stateContext.CancellationToken);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                // Игнорируем эту ошибку, т.к. сообщение не изменилось
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка в AwaitingContentState.Handle: {ex.Message}");
            await _botClient.SendMessage(stateContext.ChatId, "Произошла ошибка. Попробуйте еще раз позже.");
        }
    }
}