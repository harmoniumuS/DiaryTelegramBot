using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
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
        private readonly BotClientWrapper _botClientWrapper;
        private readonly UserDataService _userDataService;
        private readonly UserStateService _userStateService;

        public MessageHandler(BotClientWrapper botClientWrapper, UserDataService userDataService, UserStateService userStateService)
        {
            _botClientWrapper = botClientWrapper;
            _userDataService = userDataService;
            _userStateService = userStateService;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
                    await HandleCallbackQueryAsync(botClient, update.CallbackQuery,cancellationToken);
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

        private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
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
                SetStateToAwaitingContent(userId);
                return;
            }

            var userState = _userStateService.GetOrCreateState(userId);
            switch (userState.Stage)
            {
                case InputStage.AwaitingContent:
                    await HandleAwaitingContentState(botClient, chatId, userState, text, userId, cancellationToken);
                    break;

                case InputStage.AwaitingDate:
                    await HandleAwaitingDateState(botClient, chatId, userState, text, userId, cancellationToken);
                    break;

                case InputStage.AwaitingRemoveDate:
                    await HandleAwaitingRemoveDateState(botClient, chatId, userState, text, userId, cancellationToken);
                    break;

                case InputStage.AwaitingRemoveChoice:
                    await HandleAwaitingRemoveChoiceState(botClient, chatId, userState, text, userId, cancellationToken);
                    break;
            }
        }

        private async Task HandleAwaitingContentState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
        {
            userState.TempContent = text;
            userState.Stage = InputStage.AwaitingDate;
            await BotKeyboardManager.SendAddRecordsKeyboardAsync(botClient, chatId, cancellationToken,DateTime.Now);
        }

        private async Task HandleAwaitingDateState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
        {
            if (text == "/today")
            {
                var content = userState.TempContent;
                var date = DateTime.UtcNow;
                
                await _userDataService.AddOrUpdateUserDataAsync(userId, date, content);
                await _botClientWrapper.SendTextMessageAsync(chatId, "Запись добавлена!", cancellationToken);
                _userStateService.ResetUserState(userId);
            }
            else
            {
                if (DateTime.TryParse(text, out var parsedDate))
                {
                    var content = userState.TempContent;
                    await _userDataService.AddOrUpdateUserDataAsync(userId, parsedDate, content);
                    await _botClientWrapper.SendTextMessageAsync(chatId, "Запись добавлена!", cancellationToken);
                    _userStateService.ResetUserState(userId);
                }
                else
                {
                    await _botClientWrapper.SendTextMessageAsync(chatId, "Неверный формат даты, попробуйте снова.", cancellationToken);
                }
            }
        }

        private async Task HandleAwaitingRemoveDateState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
        {
            if (DateTime.TryParse(text, out var removedDate))
            {
                var records = await _userDataService.GetUserDataAsync(userId,removedDate);
                if (records.Any())
                {
                    userState.TempDate = removedDate;
                    userState.TempRecords = records;
                    if (records.Count == 1)
                    {
                        await _userDataService.RemoveUserDataAsync(userId, removedDate);
                        await _botClientWrapper.SendTextMessageAsync(
                            chatId,
                            "Единственная запись на эту дату была удалена", 
                            cancellationToken);
                    }
                    else
                    {
                        userState.Stage = InputStage.AwaitingRemoveChoice;
                        await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, records,cancellationToken);
                    }
                }
                else
                {
                    await _botClientWrapper.SendTextMessageAsync(chatId,
                        "На эту дату не найдено записей.", 
                        cancellationToken);
                    _userStateService.ResetUserState(userId);
                }
            }
            else
            {
                await _botClientWrapper.SendTextMessageAsync(
                    chatId,
                    "На эту дату не найдено записей.",
                    cancellationToken: cancellationToken);
                _userStateService.ResetUserState(userId);
            }
            
        }

        private async Task HandleAwaitingRemoveChoiceState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
        {
            if (int.TryParse(text, out int index) && index > 0 && index <= userState.TempRecords.Count)
            {
                var selectedRecord = userState.TempRecords[index - 1];
                await _userDataService.RemoveUserDataAsync(userId, userState.TempDate, selectedRecord);
                await _botClientWrapper.SendTextMessageAsync(chatId, "Запись успешно удалена!", cancellationToken);
                _userStateService.ResetUserState(userId);
            }
            else
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Неверный выбор, выберите корректный номер записи для удаления.", cancellationToken);
            }
        }
        
        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
            {
            var userId = callbackQuery?.From?.Id.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var chatId = callbackQuery.Message.Chat.Id;

            try
            {
                switch (callbackQuery.Data)
                {
                    case "add_record":
                        var userState = _userStateService.GetOrCreateState(userId);
                        userState.Stage = InputStage.AwaitingContent;

                        await HandleAddRecord(botClient, chatId, userId, cancellationToken);
                        break;

                    case "remove_record":
                        await HandleRemoveRecord(botClient, chatId, userId, cancellationToken);
                        break;

                    case "view_records":
                        await HandleViewRecords(botClient, chatId, userId, cancellationToken);
                        break;
                    case "return_main_menu":
                        await BotKeyboardManager.SendMainKeyboardAsync(botClient, chatId, cancellationToken);
                        SetStateToAwaitingContent(userId);
                        break;
                    
                    case { } dataCalendar when dataCalendar.StartsWith("calendar:day:"):
                    {
                            var datePart = dataCalendar.Substring("calendar:day:".Length);
                            if (DateTime.TryParse(datePart, out var parsedDate))
                            {
                                var userStateCalendar = _userStateService.GetOrCreateState(userId);
                                if (userStateCalendar.Stage == InputStage.AwaitingDate)
                                {
                                    userStateCalendar.TempDate = parsedDate;
                                    userStateCalendar.Stage = InputStage.None;
                                    await _botClientWrapper.SendTextMessageAsync(
                                        chatId,
                                        $"Вы выбрали дату: {parsedDate:dd.MM.yyyy}. Запись успешно добавлена.",
                                        cancellationToken: cancellationToken);
                                }

                                await _userDataService.AddOrUpdateUserDataAsync(userId, parsedDate,
                                    userStateCalendar.TempContent);
                            }
                            else
                            {
                                await botClient.SendMessage(
                                    chatId,
                                    "Некорректная дата, попробуйте ещё раз.",
                                    cancellationToken: cancellationToken);
                            }
                        break;
                    }
                    
                    case {} dataCalendarButtons when dataCalendarButtons.StartsWith("calendar:prev:") || dataCalendarButtons.StartsWith("calendar:next:"):
                        var action = dataCalendarButtons.Split(':')[0] == "calendar" ? dataCalendarButtons.Split(':')[1] : string.Empty;
                        var partOfDate = dataCalendarButtons.Substring($"calendar:{action}:".Length);
                        if (DateTime.TryParse(partOfDate, out var changeMonthDate))
                        {
                            var newDate = action == "prev" 
                                ? changeMonthDate.AddMonths(-1)
                                : changeMonthDate.AddMonths(1); 
                            var calendarMarkup = BotKeyboardManager.CreateCalendarMarkUp(newDate);
                            await botClient.SendMessage(
                                chatId,
                                $"Вы перешли к {newDate:MMMM yyyy}.",
                                replyMarkup: calendarMarkup,
                                cancellationToken: cancellationToken
                            );
                        }
                        else
                        {
                            await botClient.SendMessage(
                                chatId,
                                "Некорректная дата для перехода, попробуйте ещё раз.",
                                cancellationToken: cancellationToken);
                        }
                        break;
                    
                    case { } data when data.StartsWith("delete_"):
                        if (int.TryParse(data["delete_".Length..], out int index))
                        {
                            await HandleRemoveRecord(botClient, chatId, userId, index, cancellationToken);
                        }
                        else
                        {
                            try
                            {
                                await botClient.AnswerCallbackQuery(
                                    callbackQuery.Id,           
                                    text: "Невозможно удалить запись. Некорректный индекс.",   
                                    showAlert: true,           
                                    cancellationToken: cancellationToken
                                );
                            }
                            catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
                            {
                                Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
                            }
                            
                            return;
                        }
                        break;

                    default:
                        try
                        {
                            await botClient.AnswerCallbackQuery(
                                callbackQuery.Id,           
                                text: "Неизвестная команда.",   
                                showAlert: true,           
                                cancellationToken: cancellationToken
                            );
                        }
                        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("query is too old"))
                        {
                            Console.WriteLine("CallbackQuery is too old to answer: " + ex.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Unexpected error in AnswerCallbackQuery: " + ex.Message);
                        }
                        return;
                }

                try
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQuery.Id,           
                        text: "Запрос обработан",   
                        showAlert: false,           
                        cancellationToken: cancellationToken 
                    );
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,           
                    text: "Произошла ошибка при обработке запроса.",   
                    showAlert: true,           
                    cancellationToken: cancellationToken
                );
            }
        }
        private void SetStateToAwaitingContent(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }
            _userStateService.SetState(userId, new TempUserState { Stage = InputStage.AwaitingContent });
        }
        

        private async Task HandleAddRecord(ITelegramBotClient botClient, long chatId, string userId,
            CancellationToken cancellationToken)
        {
            var userState = _userStateService.GetOrCreateState(userId);
            await botClient.SendMessage(
                chatId: chatId,
                "Введите запись:",
                replyMarkup: new[]
                {
                    InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"),
                },
                cancellationToken: cancellationToken
            );

        }

        private async Task HandleRemoveRecord(ITelegramBotClient botClient, long chatId, string userId, CancellationToken cancellationToken)
        {
            var userData = await _userDataService.GetUserDataAsync(userId);
            
            if (userData == null || !userData.Any())
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "У вас нет записей для удаления.",cancellationToken);
                return;
            }

            var allRecords = userData
                .SelectMany(kv => kv.Value.Select(record => $"{kv.Key:yyyy-MM-dd}: {record}"))
                .ToList(); 

            _userStateService.SetState(userId, new TempUserState
            {
                Stage = InputStage.AwaitingRemoveChoice,
                TempRecords = allRecords
            });

            await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, allRecords, cancellationToken);
        }
        
        private async Task HandleRemoveRecord(ITelegramBotClient botClient, long chatId, string userId, int index, CancellationToken cancellationToken)
        {
            var state = _userStateService.GetOrCreateState(userId);
            
            if (state?.TempRecords == null || index < 0 || index >= state.TempRecords.Count)
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка с индексом записи.", cancellationToken);
                return;
            }

            var recordToDelete = state.TempRecords[index];


            var separatorIndex = recordToDelete.IndexOf(": ");
            if (separatorIndex == -1)
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка: некорректная запись.", cancellationToken);
                return;
            }

            var recordValue = recordToDelete.Substring(separatorIndex + 2);
            var allData = await _userDataService.GetUserDataAsync(userId);
            
            if (string.IsNullOrEmpty(recordToDelete))
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка: некорректная запись.", cancellationToken);
                return;
            }
            foreach (var key in allData.Keys.ToList())
            {
                if (allData[key].Remove(recordValue))
                {
                    if (!allData[key].Any())
                        allData.Remove(key);
                    break;
                }
            }

            await _userDataService.SaveUserDataAsync(userId, allData);
            await _botClientWrapper.SendTextMessageAsync(chatId, "Запись успешно удалена.", cancellationToken);

            //Еще раз почитать как это работает
            var updateRecords = allData.
                SelectMany(key=>key.Value.
                    Select(record=>$"{key.Key:yyyy-MM-dd}: {record}")).ToList();
            if (updateRecords.Any())
            {
                _userStateService.SetState(userId, new TempUserState()
                {
                    Stage = InputStage.AwaitingRemoveChoice,
                    TempRecords = updateRecords
                });
                await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, updateRecords, cancellationToken, sendIntroMessage: false);
            }
            else
            {
                _userStateService.ClearState(userId);
                await _botClientWrapper.SendTextMessageAsync(chatId, "Больше нет записей для удаления.", cancellationToken);
            }
        }

        private async Task HandleViewRecords(ITelegramBotClient botClient, long chatId, string userId,CancellationToken cancellationToken)
        {
            try
            {
                var userData = await _userDataService.GetUserDataAsync(userId);
                if (userData.Any())
                {
                    var dataString = string.Join("\n", userData.Select(r => $"{r.Key.ToString("yyyy-MM-dd")}: {string.Join(", ", r.Value)}"));
                    await _botClientWrapper.SendTextMessageAsync(chatId, dataString,cancellationToken);
                }
                else
                {
                    await _botClientWrapper.SendTextMessageAsync(chatId, "Записи не найдены!",cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных для пользователя {userId}: {ex.Message}");
                await _botClientWrapper.SendTextMessageAsync(chatId, "Произошла ошибка при обработке вашего запроса.",cancellationToken);
            }
        }
    }
}