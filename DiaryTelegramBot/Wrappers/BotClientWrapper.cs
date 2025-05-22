using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;


namespace DiaryTelegramBot.Wrappers
{
    public class BotClientWrapper
    {
        private readonly ITelegramBotClient _botClient;

        public BotClientWrapper(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public async Task SendTextMessageAsync(long chatId, string text, InlineKeyboardButton[] replyMarkup,
            CancellationToken cancellationToken)
        {
            await _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }
    }
}
