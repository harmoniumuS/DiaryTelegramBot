using DiaryTelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AddRemindHandler
{
    private readonly UserStateService _userStateService;

    public AddRemindHandler(UserStateService userStateService)
    {
        _userStateService = userStateService;
    }
    
    public async Task HandleAddRemind(ITelegramBotClient botClient, long chatId, string userId,
        CancellationToken cancellationToken)
    {
        var userState = _userStateService.GetOrCreateState(userId);

    }

}