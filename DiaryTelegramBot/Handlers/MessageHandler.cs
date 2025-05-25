using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
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


        public MessageHandler(PressedButtonHandler pressedButtonHandler
            ,UserStateHandler userStateHandler
            ,UserContext userContext)
        {
            _pressedButtonHandler = pressedButtonHandler;
            _userStateHandler = userStateHandler;
            _userContext = userContext;
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                try
                {
                    await HandleMessageAsync(botClient, update.Message, cancellationToken);
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
            CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
                                        
            if (message != null)
            {
                var chatId = message.Chat.Id;
                var text = message.Text;
                var user = await _userContext.GetUserAsync(userId);
                if (text == "/start")
                {
                    await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
                    _userStateHandler.SetState(userId,UserStatus.None);
                    return;
                }
                
                if (text != "\\start" && text != null)
                {
                    user.TempRecord.Text = text;
                    await BotKeyboardManager.SendAddRecordsKeyboardAsync(botClient,chatId,cancellationToken,DateTime.UtcNow);
                }
                await _userStateHandler.HandleState(user,chatId,cancellationToken);
            }
        }
    }
}