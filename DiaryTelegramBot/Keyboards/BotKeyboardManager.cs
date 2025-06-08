using DiaryTelegramBot.Data;
using Telegram.Bot;
using Telegram.CalendarKit;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CalendarKit.Models.Enums;

namespace DiaryTelegramBot.Keyboards
{
    public static class BotKeyboardManager
    {
        private static CalendarBuilder _calendarBuilder = new CalendarBuilder();
        public static InlineKeyboardMarkup GetMainKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Добавить запись", "add_record"),
                    InlineKeyboardButton.WithCallbackData("Удалить запись", "remove_record"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Посмотреть все записи", "view_records"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Добавить напоминание", "add_reminder"),
                    InlineKeyboardButton.WithCallbackData("Удалить напоминание", "remove_reminder"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Посмотреть все напоминания", "view_reminders"),
                }
            });
        }

        public static async Task SendMainKeyboardAsync(ITelegramBotClient botClient, StateContext stateContext)
        {
            var inlineKeyboard = GetMainKeyboard();

            await botClient.SendMessage(
                stateContext.ChatId,
                "Выберите действие:",
                replyMarkup: inlineKeyboard,
                cancellationToken: stateContext.CancellationToken);
        }

        public static async Task SendAddRemindersKeyboard(ITelegramBotClient botClient, long chatId, List<string> records, CancellationToken cancellationToken)
        {
                if (records.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "Нет записей для добавления напоминаний.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var keyboard = new InlineKeyboardMarkup(
                    records.Select((record, index) =>
                        {
                            var displayText = record.Length > 30
                                ? record.Substring(0, 30) + "..."
                                : record;

                            return new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"{index + 1}. {displayText}", $"add_remind_{index}")
                            };
                        })
                        .Append(new[]
                            { InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu") })
                        .ToArray());

                await botClient.SendMessage(
                    chatId,
                    "Выберите запись для добавления напоминания:",
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);

        }

        public static InlineKeyboardMarkup GetReminderKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("За 5 минут", "remind_offset_5") },
                new[] { InlineKeyboardButton.WithCallbackData("За 30 минут", "remind_offset_30") },
                new[] { InlineKeyboardButton.WithCallbackData("За 1 час", "remind_offset_60") },
                new[] { InlineKeyboardButton.WithCallbackData("За 24 часа", "remind_offset_1440") }
            });
        }

        
        public static async Task SendRemoveKeyboardAsync(ITelegramBotClient botClient, long chatId, List<string> records, CancellationToken cancellationToken, 
            bool sendIntroMessage = true,bool iSReminderKeyboard =false)
        {
            if (records.Count == 0)
            {
                await botClient.SendMessage(
                    chatId,
                    "Нет записей для удаления.",
                    cancellationToken: cancellationToken);
                return;
            }

            if (iSReminderKeyboard)
            {
                var reminderKeyboard = new InlineKeyboardMarkup(
                    records.Select((record, index) =>
                        {
                            var displayText = record.Length > 30
                                ? record.Substring(0, 30) + "..."
                                : record;

                            return new[] { InlineKeyboardButton.WithCallbackData($"{index + 1}. {displayText}", $"deleteReminder_{index}") };
                        })
                        .Append(new[] { InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu") })
                        .ToArray()
                );
                var messageText = sendIntroMessage
                    ? "Выберите запись для удаления:"
                    : "Обновлённый список записей:";

                await botClient.SendMessage(
                    chatId,
                    messageText,
                    replyMarkup: reminderKeyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var keyboard = new InlineKeyboardMarkup(
                    records.Select((record, index) =>
                        {
                            var displayText = record.Length > 30
                                ? record.Substring(0, 30) + "..."
                                : record;

                            return new[] { InlineKeyboardButton.WithCallbackData($"{index + 1}. {displayText}", $"delete_{index}") };
                        })
                        .Append(new[] { InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu") })
                        .ToArray()
                );
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
        public static async Task SendDataKeyboardAsync(ITelegramBotClient botClient, long chatId, int callBackMessageId,
            CancellationToken cancellationToken, DateTime date)
        {
            var keyboard = BuildCalendarKeyboard(date);

            await botClient.EditMessageText(
                chatId: chatId,
                messageId: callBackMessageId,
                text: $"Выберите дату. Выбран: {date:MMMM yyyy}.",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        
        public static InlineKeyboardMarkup BuildCalendarKeyboard(DateTime date)
        {
            var calendarButtons = _calendarBuilder.GenerateCalendarButtons(
                date.Year,
                date.Month,
                CalendarViewType.Weekly,
                "ru");

            var buttons = calendarButtons.InlineKeyboard.ToList();
            buttons.Add([InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu")]);

            return new InlineKeyboardMarkup(buttons);
        }
        public static async Task SendTimeMarkUp(ITelegramBotClient botClient,StateContext stateContext)
        {
            var time = Enumerable.Range(0, 24)
                .Select(h => $"{h:D2}:00")
                .ToList();

            var buttons = time
                .Select(t =>
                {
                    var hour = t.Split(':')[0];
                    var callback = $"time_hour_{hour}";
                    return InlineKeyboardButton.WithCallbackData(t, callback);
                })
                .Chunk(4)
                .Select(row => row.ToArray())
                .ToList();

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu") });

            var timeKeyboard = new InlineKeyboardMarkup(buttons);
            await botClient.EditMessageText(stateContext.ChatId,stateContext.CallBackQueryId, $"Вы выбрали дату: {stateContext.TempRecord.SentTime:dd.MM.yyyy}.Теперь выберите время:", replyMarkup: timeKeyboard, cancellationToken: stateContext.CancellationToken);
        }


        public static InlineKeyboardMarkup SendMinutesMarkUp(int selectedHour)
        {
            var minuteButtons = Enumerable.Range(0, 60)
                .Where(min => min % 5 == 0)
                .Select(min =>
                {
                    var label = $"{selectedHour:D2}:{min:D2}";
                    var callback = $"time_minute_{selectedHour:D2}:{min:D2}"; 
                    return InlineKeyboardButton.WithCallbackData(label, callback);
                })
                .Chunk(4)
                .Select(row => row.ToArray())
                .ToList();

            minuteButtons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("<< Назад", "return_hour_selection")
            });

            return new InlineKeyboardMarkup(minuteButtons);
        }
        
    }
    
    
}
