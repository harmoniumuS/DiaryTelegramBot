using System.Net;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot;

public class UserStateService
{
    private readonly Dictionary<string,TempUserState> _userStates = new();
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserDataService _userDataService;
    private readonly UserStateService _userStateService;
        

    public UserStateService(BotClientWrapper botClientWrapper, UserDataService userDataService, 
        UserStateService userStateService)
    {
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
        _userStateService = userStateService;
            
    }
    public TempUserState GetOrCreateState(string userId)
    {
        if (!_userStates.ContainsKey(userId))
        {
            _userStates[userId] = new TempUserState { Stage = InputStage.None };
        }
        
        return _userStates[userId];
    }
    public void SetState(string userId, TempUserState state)
    {
        _userStates[userId] = state;
    }
    
    public void ResetUserState(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }
        SetState(userId, new TempUserState { Stage = InputStage.None });
    }
    public void ClearState(string userId)
    {
        _userStates.Remove(userId);
    }
    public void SetStateToAwaitingContent(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }
        SetState(userId, new TempUserState { Stage = InputStage.AwaitingContent });
    }
     public async Task HandleAwaitingContentState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
        {
            userState.TempContent = text;
            userState.Stage = InputStage.AwaitingDate;
            await BotKeyboardManager.SendAddRecordsKeyboardAsync(botClient, chatId, cancellationToken,DateTime.Now);
        }

        public async Task HandleAwaitingDateState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
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

        public async Task HandleAwaitingRemoveDateState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
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
        public async Task HandleAwaitingRemoveChoiceState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
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
    }
    