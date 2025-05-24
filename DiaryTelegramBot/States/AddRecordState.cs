using DiaryTelegramBot.Data;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AddRecordState : IState
{
    private readonly UserStateHandler _userStateHandler;
    private readonly BotClientWrapper _botClientWrapper;

    public AddRecordState(BotClientWrapper botClientWrapper, UserStateHandler userStateHandler)
    {
        _botClientWrapper = botClientWrapper;
        _userStateHandler = userStateHandler;
    }

    public async Task Handle(UserStateHandler stateHandler, User user,
        CancellationToken cancellationToken)
    {
        await _botClientWrapper.SendTextMessageAsync(
            user.Id,
            "Введите запись:",
            replyMarkup: new[]
            {
                InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"),
            },
            cancellationToken);
        ;
        stateHandler.SetState(user);
    }
}