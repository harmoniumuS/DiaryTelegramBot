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
    public AwaitingContentState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        if (!string.IsNullOrWhiteSpace(stateContext.MessageText))
        {

            if (stateContext.User.TempRecord == null)
            {
                stateContext.User.TempRecord = new Record();
            }

            stateContext.User.TempRecord.Text = stateContext.MessageText;
            stateContext.User.CurrentStatus = UserStatus.AwaitingDate;
            await BotKeyboardManager.SendDataKeyboardAsync(_botClient, stateContext.ChatId, stateContext.CallBackQueryId,stateContext.CancellationToken,DateTime.Now);
            return;
        }

        var replyMarkup = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"));

        await _botClient.EditMessageText(
            stateContext.ChatId,
            messageId: stateContext.CallBackQueryId,
            "Введите запись:",
            replyMarkup: replyMarkup,
            cancellationToken: stateContext.CancellationToken);
    }
}