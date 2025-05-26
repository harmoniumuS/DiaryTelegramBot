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
       
        await _botClient.SendMessage(
            chatId,
            "Введите запись:",
            replyMarkup: new[]
            {
                InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"),
            });
    }
}