using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AwaitingContentState:IState
{
    private readonly ITelegramBotClient _botClient;
    public AwaitingContentState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken,string dataHandler = null)
    {
        if (!string.IsNullOrWhiteSpace(dataHandler))
        {
            user.TempRecord.Text = dataHandler;
            user.CurrentStatus = UserStatus.AwaitingDate;
            BotKeyboardManager.SendDataKeyboardAsync(_botClient, chatId, cancellationToken,DateTime.Now);
            return;
        }

        var replyMarkup = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"));

        await _botClient.SendMessage(
            chatId,
            "Введите запись:",
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }
}