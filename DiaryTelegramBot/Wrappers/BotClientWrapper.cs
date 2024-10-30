using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async Task SendTextMessageAsync(long chatId,string text,CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
        }

        public async Task SendInlineKeyboardAsync(long chatId, string text,InlineKeyboardMarkup keyboard,CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

    }
}
