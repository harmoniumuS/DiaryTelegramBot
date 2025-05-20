using Telegram.Bot;


namespace DiaryTelegramBot.Wrappers
{
    public class BotClientWrapper
    {
        private readonly ITelegramBotClient _botClient;

        public BotClientWrapper(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public async Task SendTextMessageAsync(long chatId,string text,CancellationToken cancellationToken)
        {
            await _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }
    }
}
