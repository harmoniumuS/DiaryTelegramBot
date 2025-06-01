using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

public class AwaitingMainMenuState : IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingMainMenuState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(User user, long chatId, CancellationToken cancellationToken, string dataHandler = null)
    {
        await BotKeyboardManager.SendMainKeyboardAsync(_botClient, chatId, cancellationToken);
    }
}