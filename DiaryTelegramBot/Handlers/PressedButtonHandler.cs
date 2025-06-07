using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiaryTelegramBot.Handlers;

public class PressedButtonHandler
{
    private readonly UserStateHandler _userStateHandler;
    private readonly UserContext _userContext;
    private readonly IMemoryCache _memoryCache;

    public PressedButtonHandler(
        UserStateHandler userStateHandler,
        UserContext userContext,
        IMemoryCache memoryCache)
    {
        _userStateHandler = userStateHandler;
        _userContext = userContext;
        _memoryCache = memoryCache;
    }

    public async Task HandlePressedButtonAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery?.Message == null) return;

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;
        var callbackData = callbackQuery.Data ?? "";
        var messageId = callbackQuery.Message.MessageId;

        var user = await _memoryCache.GetOrCreateAsync($"user_{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _userContext.GetUserAsync(userId);
        });

        var stateContext = new StateContext(
            user,
            chatId,
            cancellationToken,
            callbackData,
            callBackQueryId:messageId
        );

        try
        {
            if (callbackData == "return_main_menu")
            {
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "Выберите действие:",
                    replyMarkup: BotKeyboardManager.GetMainKeyboard(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _userStateHandler.HandleState(stateContext);

                _memoryCache.Set($"user_{userId}", user, TimeSpan.FromMinutes(10));
            }

            try
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, text: "Запрос обработан", showAlert: false, cancellationToken: cancellationToken);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
            {
                Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
            try
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, text: "Произошла ошибка при обработке запроса.", showAlert: true, cancellationToken: cancellationToken);
            }
            catch
            {
                // 
            }
        }
    }
}
