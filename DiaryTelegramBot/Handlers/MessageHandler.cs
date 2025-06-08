using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = Telegram.Bot.Types.Message;

namespace DiaryTelegramBot.Handlers
{
    public class MessageHandler
    {
        private readonly PressedButtonHandler _pressedButtonHandler;
        private readonly UserStateHandler _userStateHandler;
        private readonly UserContext _userContext;
        private readonly IMemoryCache _memoryCache;
        
        public MessageHandler(PressedButtonHandler pressedButtonHandler,
            UserStateHandler userStateHandler,
            UserContext userContext,
            IMemoryCache memoryCache)
        {
            _pressedButtonHandler = pressedButtonHandler;
            _userStateHandler = userStateHandler;
            _userContext = userContext;
            _memoryCache = memoryCache;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        { 
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                try
                {
                    var userId = update.Message.From?.Id;
                    if (userId == null) return;
                    
                    if (!_memoryCache.TryGetValue<StateContext>($"state_{userId}", out var stateContext))
                    {
                        
                        var user = await _userContext.GetUserAsync(userId.Value);
                        stateContext = new StateContext(
                            user,
                            update.Message.Chat.Id,
                            cancellationToken,
                            callbackData: null,
                            messageText: update.Message.Text,
                            callBackQueryId: 0);
                    }
                    else
                    {
                        if (stateContext != null)
                        {
                            stateContext.MessageText = update.Message.Text;
                            stateContext.CancellationToken = cancellationToken;
                        }
                    }

                    await HandleMessageAsync(botClient, update.Message, cancellationToken, stateContext);
                    _memoryCache.Set($"state_{userId}", stateContext, TimeSpan.FromMinutes(10));
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
            else if (update.Type == UpdateType.CallbackQuery)
            {
                try
                {
                    await _pressedButtonHandler.HandlePressedButtonAsync(botClient, update.CallbackQuery,
                        cancellationToken);
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
        private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message,
            CancellationToken cancellationToken, StateContext stateContext)
        {
            if (message.From != null)
            {
                var chatId = message.Chat.Id;
                var text = message.Text;
                if (text == "/start")
                {
                    await BotKeyboardManager.SendMainKeyboardAsync(botClient, stateContext);
                    return;
                }
                await _userStateHandler.HandleState(stateContext);
            }
        }
    }
}
