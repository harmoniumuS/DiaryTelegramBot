using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.CalendarKit;

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
                await HandleMessageAsync(botClient, update.Message, cancellationToken);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery,cancellationToken);
            }
        }

        private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var userId = message?.From?.Id.ToString(); // Ensure userId is not null
            if (string.IsNullOrEmpty(userId))
            {
                // Log error or handle the case where userId is null
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
            await _botClientWrapper.SendTextMessageAsync(chatId, "Введите дату в формате ГГГГ-ММ-ДД, или нажмите /today для сегодняшней:", cancellationToken);
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
                await HandleAddRecord(botClient, chatId, userId, cancellationToken);
                break;

            case "remove_record":
                await HandleRemoveRecord(botClient, chatId, userId, cancellationToken);
                break;

            case "view_records":
                await HandleViewRecords(botClient, chatId, userId, cancellationToken);
                break;

            case { } data when data.StartsWith("delete_"):
                if (int.TryParse(data["delete_".Length..], out int index))
                {
                    await HandleRemoveRecord(botClient, chatId, userId, index, cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQuery.Id,           
                        text: "Невозможно удалить запись. Некорректный индекс.",   
                        showAlert: true,           
                        cancellationToken: cancellationToken
                    );
                    return;
                }
                break;

            default:
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,           
                    text: "Неизвестная команда.",   
                    showAlert: true,           
                    cancellationToken: cancellationToken
                );
                return;
        }
        
        await botClient.AnswerCallbackQuery(
            callbackQuery.Id,           
            text: "Запрос обработан",   
            showAlert: false,           
            cancellationToken: cancellationToken 
        );
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
        

        private async Task HandleAddRecord(ITelegramBotClient botClient, long chatId, string userId,CancellationToken cancellationToken)
        {
            SetStateToAwaitingContent(userId);
            await _botClientWrapper.SendTextMessageAsync(chatId, "Введите запись:",cancellationToken);
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
            var allData = await _userDataService.GetUserDataAsync(userId);
            
            if (string.IsNullOrEmpty(recordToDelete))
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "Ошибка: некорректная запись.", cancellationToken);
                return;
            }
            
            var dataToRemove = allData.FirstOrDefault(key => key.Value.Contains(recordToDelete));
            if (dataToRemove.Key != null)
            {
                allData.Remove(dataToRemove.Key);
            }

            foreach (var key in allData.Keys.ToList())
            {
                if (allData[key].Remove(recordToDelete))
                {
                    if (!allData[key].Any())
                        allData.Remove(key);
                    break;
                }
            }

            await _userDataService.SaveUserDataAsync(userId, allData);
            await _botClientWrapper.SendTextMessageAsync(chatId, "Запись успешно удалена.", cancellationToken);

            var updateRecords = allData.SelectMany(key => key.Value).ToList();
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