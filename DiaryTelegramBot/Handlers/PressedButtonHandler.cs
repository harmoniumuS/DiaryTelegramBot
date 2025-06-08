using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiaryTelegramBot.Handlers
{
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

        public async Task HandlePressedButtonAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery,
            CancellationToken cancellationToken)
        {
            if (callbackQuery?.Message == null) return;

            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var callbackData = callbackQuery.Data ?? "";
            var messageId = callbackQuery.Message.MessageId;

            // Попытка получить stateContext из кэша
            if (!_memoryCache.TryGetValue<StateContext>($"state_{userId}", out var stateContext))
            {
                var user = await _memoryCache.GetOrCreateAsync($"user_{userId}", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    return await _userContext.GetUserAsync(userId);
                });

                stateContext = new StateContext(
                    user,
                    chatId,
                    cancellationToken,
                    callbackData,
                    messageText: null,
                    callBackQueryId: messageId
                );

                _memoryCache.Set($"state_{userId}", stateContext, TimeSpan.FromMinutes(10));
            }
            else
            {
                stateContext.CallbackData = callbackData;
                stateContext.CallBackQueryId = messageId;
            }

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
                    _memoryCache.Set($"user_{userId}", stateContext.User, TimeSpan.FromMinutes(10));
                }

                try
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, text: "Запрос обработан",
                        showAlert: false, cancellationToken: cancellationToken);
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
                {
                    Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected error while answering callback query: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"Ошибка при обработке callback-запроса (UserId: {userId}, Data: '{callbackData}'): {ex}");

                if (!string.IsNullOrEmpty(callbackQuery.Id))
                {
                    try
                    {
                        await botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            text: "Произошла ошибка при обработке запроса. Попробуйте позже.",
                            showAlert: true,
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception innerEx)
                    {
                        Console.Error.WriteLine($"Ошибка при отправке ответа на callback-запрос: {innerEx}");
                    }
                }
            }
        }
    }
}
