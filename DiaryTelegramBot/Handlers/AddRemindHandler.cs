using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers
{
    public class AddRemindHandler
    {
        private readonly UserDataService _userDataService;
        private readonly BotClientWrapper _botClientWrapper;

        public AddRemindHandler( BotClientWrapper botClientWrapper, UserDataService userDataService)
        {
            _botClientWrapper = botClientWrapper;
            _userDataService = userDataService;
        }

        public async Task HandleAddRemind(ITelegramBotClient botClient, long chatId, string userId, CancellationToken cancellationToken)
        {
            try
            {
                var userData = _userDataService.GetUserDataAsync(userId).Result;
                var allRecords = userData
                    .SelectMany(kv => kv.Value.Select(record => $"{kv.Key:yyyy-MM-dd HH:mm}: {record}"))
                    .ToList();
                
                var userState = _userStateService.GetOrCreateState(userId);
                
                userState.Stage = UserStatus.AwaitingRemind;
                userState.TempRecords = allRecords;
                
                _userStateService.SaveState(userId, userState);

                await BotKeyboardManager.SendAddRemindersKeyboard(botClient, chatId, allRecords, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка при получении данных для пользователя {userId}: {e.Message}");
                await _botClientWrapper.SendTextMessageAsync(chatId, "Произошла ошибка при обработке вашего запроса.", cancellationToken);
            }
        }

        public async Task HandleAddRemind(ITelegramBotClient botClient, long chatId, string userId, int index,
            CancellationToken cancellationToken)
        {
            var userState = _userStateService.GetOrCreateState(userId);
    
            if (userState.Stage != UserStatus.AwaitingRemind || userState.TempRecords == null || index < 0 || index >= userState.TempRecords.Count)
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Некорректный выбор записи.", cancellationToken);
                return;
            }
            
            var selectedRecord = userState.TempRecords[index];
            
            var recordDateTimeString = selectedRecord.Substring(0, 16); 
    
            if (DateTime.TryParseExact(recordDateTimeString, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime selectedDateTime))
            {
                userState.TempDate = selectedDateTime.Date;
                userState.TempTime = selectedDateTime.TimeOfDay;
                userState.TempContent =  selectedRecord.Split(" ")[2];

                await botClient.SendMessage(chatId,
                    text: $"Вы выбрали: {selectedRecord}\nТеперь выберите смещение времени.",
                    replyMarkup: BotKeyboardManager.GetReminderKeyboard(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка при извлечении времени из записи.", cancellationToken);
            }
        }
        
        public async Task HandleRemindOffset(ITelegramBotClient botClient, long chatId, string userId, int offsetMinutes, CancellationToken cancellationToken)
        {
            var userState = _userStateService.GetOrCreateState(userId);
            
            if (userState.TempDate == DateTime.MinValue || !userState.TempTime.HasValue)
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка: не выбрана дата или время.", cancellationToken);
                return;
            }

            var remindTime = userState.TempDate.Date + userState.TempTime.Value;
            
            remindTime = remindTime.AddMinutes(offsetMinutes);
            
            userState.TempTime = remindTime.TimeOfDay;
            
            _userStateService.SaveState(userId, userState);

            
            var reminder = new Remind
            {
                UserId = int.Parse(userId),
                Time = remindTime,
                Message = userState.TempContent,
                IsRemind = false
            };
            await _userDataService.SaveRemindDataAsync(userId, reminder);
            switch (offsetMinutes)
            {
                case -5:
                    await _botClientWrapper.SendTextMessageAsync(
                        chatId,
                        $"Напоминание установлено на {remindTime:dd.MM.yyyy HH:mm}, я напомню за 5 минут до события.",
                        cancellationToken: cancellationToken);
                    break;
                case -30:
                    await _botClientWrapper.SendTextMessageAsync(
                        chatId,
                        $"Напоминание установлено на {remindTime:dd.MM.yyyy HH:mm}, я напомню за 30 минут до события.",
                        cancellationToken: cancellationToken);
                    break;
                case -60:
                    await _botClientWrapper.SendTextMessageAsync(
                        chatId,
                        $"Напоминание установлено на {remindTime:dd.MM.yyyy HH:mm}, я напомню за час  до события.",
                        cancellationToken: cancellationToken);
                    break;
                case -1440:
                    await _botClientWrapper.SendTextMessageAsync(
                        chatId,
                        $"Напоминание установлено на {remindTime:dd.MM.yyyy HH:mm}, я напомню за 24 часа до события.",
                        cancellationToken: cancellationToken);
                    break;
            }
            
        }

    }
}
