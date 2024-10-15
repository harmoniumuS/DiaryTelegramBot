using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Keyboards
{
    public static class BotKeyboardManager
    {
        public static async Task SendMainKeyboardAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
        new []
        {
            InlineKeyboardButton.WithCallbackData("Добавить запись", "add_record"),
            InlineKeyboardButton.WithCallbackData("Удалить запись", "remove_record"),
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData("Посмотреть все записи", "view_records"),
        }
    });

            await botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }
    }
}
