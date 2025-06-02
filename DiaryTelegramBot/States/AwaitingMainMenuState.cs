using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Telegram.Bot;

namespace DiaryTelegramBot.States;

public class AwaitingMainMenuState : IState
{
    private readonly ITelegramBotClient _botClient;

    public AwaitingMainMenuState(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        await BotKeyboardManager.SendMainKeyboardAsync(_botClient, stateContext.ChatId, stateContext.CancellationToken);
    }
}