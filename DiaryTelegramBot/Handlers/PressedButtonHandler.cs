using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiaryTelegramBot.Handlers;

public class PressedButtonHandler(
    UserStateHandler userStateHandler,
    UserContext userContext,
    IMemoryCache memoryCache)
{
    public async Task HandlePressedButtonAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery?.Message == null) return;

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;
        var callbackData = callbackQuery.Data ?? "";
        var user = await memoryCache.GetOrCreateAsync($"user_{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await userContext.GetUserAsync(userId);
        });
    
        var stateContext = new StateContext(
            user,
            chatId,
            cancellationToken,
            callbackData
        );
        try
        {
            if (callbackData == "return_main_menu")
            {
                await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
            }
            else
            {
                await userStateHandler.HandleState(stateContext);
                memoryCache.Set($"user_{userId}", user, TimeSpan.FromMinutes(10));
            }

            await SafeAnswerCallback(botClient, callbackQuery.Id, "Запрос обработан", cancellationToken, showAlert: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
            await SafeAnswerCallback(botClient, callbackQuery.Id, "Произошла ошибка при обработке запроса.", cancellationToken, showAlert: true);
        }
    }
    private async Task SafeAnswerCallback(ITelegramBotClient botClient, string callbackQueryId, string text, CancellationToken cancellationToken, bool showAlert = true)
    {
        try
        {
            await botClient.AnswerCallbackQuery(callbackQueryId, text: text, showAlert: showAlert, cancellationToken: cancellationToken);
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
}

