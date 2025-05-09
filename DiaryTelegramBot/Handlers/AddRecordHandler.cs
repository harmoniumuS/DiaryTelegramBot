using DiaryTelegramBot.Data;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AddRecordHandler
{
    private readonly UserStateService _userStateService;
    public AddRecordHandler(UserStateService userStateService)
    {
        _userStateService = userStateService;
    }

    public async Task HandleAddRecord(ITelegramBotClient botClient, long chatId, string userId,
        CancellationToken cancellationToken)
    {
        var userState = _userStateService.GetOrCreateState(userId);
        await botClient.SendMessage(
            chatId: chatId,
            "Введите запись:",
            replyMarkup: new[]
            {
                InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"),
            },
            cancellationToken: cancellationToken
        );

    }
}