using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;

public class UserStateHandler
{
    private Dictionary<long, UserStatus> _states = new();
    private readonly UserDataService _userDataService;
    private readonly BotClientWrapper _botClientWrapper;
    private readonly State _state;

    public UserStateHandler(BotClientWrapper botClientWrapper, UserDataService userDataService, State state)
    {
        _userDataService = userDataService;
        _botClientWrapper = botClientWrapper;
        _state = state;
    }
    
    public UserStatus GetState(long userId)
    {
        if (!_states.ContainsKey(userId))
            _states[userId] = UserStatus.None; 

        return _states[userId];
    }
    public void SetState(long userId, UserStatus state)
    {
        _states[userId] = state;
    }
}

/*
public async Task HandleAwaitingContentState(
    ITelegramBotClient botClient,
    long chatId,
    string? text,
    long userId,
    CancellationToken cancellationToken)
{
    var userState = _state.GetState(userId);
    userState.TempContent = text;
    userState.Stage = UserStatus.AwaitingDate;

    await BotKeyboardManager.SendAddRecordsKeyboardAsync(botClient, chatId, cancellationToken, DateTime.Now);
}

public async Task HandleAwaitingTimeState(
    ITelegramBotClient botClient,
    long chatId,
    string? text,
    long userId,
    CancellationToken cancellationToken)
{
    var userState = _state.GetState(userId);

    if (TimeSpan.TryParse(text, out var parsedTime))
    {
        userState.TempTime = parsedTime;
        userState.Stage = UserStatus.None;

        if (userState.TempDate != DateTime.MinValue)
        {
            var finalDateTime = userState.TempDate.Date + userState.TempTime.Value;

            await _botClientWrapper.SendTextMessageAsync(
                chatId,
                $"Запись '{userState.TempContent}' сделана на дату: {finalDateTime:dd.MM.yyyy HH:mm}.",
                cancellationToken: cancellationToken);

            await _userDataService.AddOrUpdateUserDataAsync(userId.ToString(), finalDateTime, userState.TempContent!);

            // Очистка временных данных
            userState.TempDate = DateTime.MinValue;
            userState.TempTime = null;
            userState.TempContent = null;
        }
        else
        {
            await _botClientWrapper.SendTextMessageAsync(
                chatId,
                "Дата не выбрана. Пожалуйста, выберите дату.",
                cancellationToken: cancellationToken);
        }
    }
    else
    {
        await _botClientWrapper.SendTextMessageAsync(
            chatId,
            "Некорректный формат времени. Введите время в формате HH:mm, например 14:30.",
            cancellationToken: cancellationToken);
    }
}

public async Task HandleAwaitingRemoveDateState(
    ITelegramBotClient botClient,
    long chatId,
    long userId,
    string? text,
    CancellationToken cancellationToken)
{
    var userState = _state.GetOrCreate(userId);

    if (DateTime.TryParse(text, out var removedDate))
    {
        var records = await _userDataService.GetUserDataAsync(userId.ToString(), removedDate);

        if (records.Any())
        {
            userState.TempDate = removedDate;
            userState.TempRecords = records;

            if (records.Count == 1)
            {
                await _userDataService.RemoveUserDataAsync(userId.ToString(), removedDate);
                await _botClientWrapper.SendTextMessageAsync(chatId, "Единственная запись на эту дату была удалена", cancellationToken);
                _state.ResetState(userId);
            }
            else
            {
                userState.Stage = UserStatus.AwaitingRemoveChoice;
                await BotKeyboardManager.SendRemoveKeyboardAsync(botClient, chatId, records, cancellationToken);
            }
        }
        else
        {
            await _botClientWrapper.SendTextMessageAsync(chatId, "На эту дату не найдено записей.", cancellationToken);
            _state.ResetState(userId);
        }
    }
    else
    {
        await _botClientWrapper.SendTextMessageAsync(chatId, "Некорректная дата.", cancellationToken);
        _state.ResetState(userId);
    }
}

public async Task HandleAwaitingRemoveChoiceState(
    ITelegramBotClient botClient,
    long chatId,
    long userId,
    string? text,
    CancellationToken cancellationToken)
{
    var userState = _state.GetOrCreate(userId);

    if (int.TryParse(text, out int index) && index > 0 && userState.TempRecords != null && index <= userState.TempRecords.Count)
    {
        var selectedRecord = userState.TempRecords[index - 1];
        await _userDataService.RemoveUserDataAsync(userId.ToString(), userState.TempDate, selectedRecord);
        await _botClientWrapper.SendTextMessageAsync(chatId, "Запись успешно удалена!", cancellationToken);
        _state.ResetState(userId);
    }
    else
    {
        await _botClientWrapper.SendTextMessageAsync(chatId, "Неверный выбор, выберите корректный номер записи для удаления.", cancellationToken);
    }
}
}
*/
