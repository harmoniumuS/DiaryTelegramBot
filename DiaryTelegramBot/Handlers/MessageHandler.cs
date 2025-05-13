using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CalendarKit;
using Telegram.CalendarKit.Models;
using Telegram.CalendarKit.Models.Enums;

namespace DiaryTelegramBot.Handlers
{
    public class MessageHandler
    {
        private readonly UserStateService _userStateService;
        private readonly CallBackQueryHandler _callBackQueryHandler;
        private readonly UserStateHandler _userStateHandler;


        public MessageHandler(UserStateService userStateService, CallBackQueryHandler callBackQueryHandler,UserStateHandler userStateHandler)
        {
            _userStateService = userStateService;
            _callBackQueryHandler = callBackQueryHandler;
            _userStateHandler = userStateHandler;   
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
                    await _callBackQueryHandler.HandleCallbackQueryAsync(botClient, update.CallbackQuery,
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
            var userId = message?.From?.Id.ToString();
            if (string.IsNullOrEmpty(userId))
            {

                return;
            }

            var chatId = message.Chat.Id;
            var text = message.Text;

            if (text == "/start")
            {
                await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
                _userStateService.SetStateToAwaitingContent(userId);
                return;
            }
            
            var userState = _userStateService.GetOrCreateState(userId);
            switch (userState.Stage)
            {
                case InputStage.AwaitingContent:

                    await _userStateHandler.HandleAwaitingContentState(botClient, chatId, userState, text, userId,
                        cancellationToken);
                    break;
                case InputStage.AwaitingDate:
                    await _userStateHandler.HandleAwaitingDateState(chatId, userState, text, userId,
                        cancellationToken);
                    break;
                case InputStage.AwaitingTime:
                    await _userStateHandler.HandleAwaitingTimeState(chatId, userState, text, userId,cancellationToken);
                    break;
                case InputStage.AwaitingRemoveDate:
                    await _userStateHandler.HandleAwaitingRemoveDateState(botClient, chatId, userState, text, userId,
                        cancellationToken);
                    break;
                case InputStage.AwaitingRemoveChoice:
                    await _userStateHandler.HandleAwaitingRemoveChoiceState(botClient, chatId, userState, text, userId,
                        cancellationToken);
                    break;
            }
        }
    }
}