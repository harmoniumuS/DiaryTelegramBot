using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;

public class UserStateHandler
{
    private readonly UserStateService _userStateService;
    private readonly UserDataService _userDataService;
    private readonly BotClientWrapper _botClientWrapper;

    public UserStateHandler(UserStateService userStateService,BotClientWrapper botClientWrapper,UserDataService userDataService)
    {
        _userStateService = userStateService;
        _userDataService = userDataService;
        _botClientWrapper = botClientWrapper;
    }

    public async Task HandleAwaitingContentState(ITelegramBotClient botClient, long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
        {
            userState.TempContent = text;
            userState.Stage = InputStage.AwaitingDate;
            Console.WriteLine($"User {userId} current stage: {userState.Stage}");
            await BotKeyboardManager.SendAddRecordsKeyboardAsync(botClient, chatId, cancellationToken,DateTime.Now);
        }

        public async Task HandleAwaitingDateState(long chatId, TempUserState userState, string text, string userId, CancellationToken cancellationToken)
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
                    Console.WriteLine($"User {userId} current stage: {userState.Stage}");
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