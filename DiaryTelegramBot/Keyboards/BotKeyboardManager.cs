using Telegram.Bot;
using Telegram.CalendarKit;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Keyboards
{
    public static class BotKeyboardManager
    {
        public static async Task SendMainKeyboardAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Добавить запись", "add_record"),
                    InlineKeyboardButton.WithCallbackData("Удалить запись", "remove_record"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Посмотреть все записи", "view_records"),
                }
            });

            await botClient.SendMessage(chatId,"Выберите действие:",replyMarkup:inlineKeyboard,cancellationToken:cancellationToken);
        }
        public static async Task SendRemoveKeyboardAsync(ITelegramBotClient botClient, long chatId, List<string> records, CancellationToken cancellationToken, 
            bool sendIntroMessage = true)
        {
            if (records == null || records.Count == 0)
            {
                await botClient.SendMessage(
                    chatId,
                    "Нет записей для удаления.",
                    cancellationToken: cancellationToken);
                return;
            }

            var keyboard = new InlineKeyboardMarkup(
                records.Select((record, index) =>
                {
                    var displayText = record.Length > 30
                        ? record.Substring(0, 30) + "..."
                        : record;

                    return new[] { InlineKeyboardButton.WithCallbackData($"{index + 1}. {displayText}", $"delete_{index}") };
                }));

            var messageText = sendIntroMessage
                ? "Выберите запись для удаления:"
                : "Обновлённый список записей:";

            await botClient.SendMessage(
                chatId,
                messageText,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }


    }
    
    
}
