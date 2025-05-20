using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class RemoveRecordHandler
{
    private readonly BotClientWrapper _botClientWrapper;
    private readonly UserDataService _userDataService;
    private readonly UserStateService _userStateService;

    public RemoveRecordHandler(BotClientWrapper botClientWrapper, UserDataService userDataService, 
        UserStateService userStateService)
    {
        _botClientWrapper = botClientWrapper;
        _userDataService = userDataService;
        _userStateService = userStateService;
    }
    public async Task HandleRemoveRecord(ITelegramBotClient botClient, long chatId, string userId, CancellationToken cancellationToken)
        {
            var userData = await _userDataService.GetUserDataAsync(userId);
            
            if (userData == null || !userData.Any())
            {
                await _botClientWrapper.SendTextMessageAsync(chatId, "У вас нет записей для удаления.",cancellationToken);
                return;
            }

            var allRecords = userData
                .SelectMany(kv => kv.Value.Select(record => $"{kv.Key:yyyy-MM-dd HH:mm}: {record}"))
                .ToList(); 

            _userStateService.SetState(userId, new TempUserState
            {
                Stage = InputStage.AwaitingRemoveChoice,
                TempRecords = allRecords
            });

            await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, allRecords, cancellationToken);
        }
        
        public async Task HandleRemoveRecord(ITelegramBotClient botClient, long chatId, string userId, int index, CancellationToken cancellationToken)
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
}