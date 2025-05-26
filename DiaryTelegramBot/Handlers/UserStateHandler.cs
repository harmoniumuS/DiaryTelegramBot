using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

public class UserStateHandler
{
    private readonly Dictionary<UserStatus, IState> _states;
    private readonly AddRemindState _addRemindState;
    private readonly ITelegramBotClient _client;
    private readonly UserContext _userContext;

    public UserStateHandler(
        AddRecordState addRecordState,
        RemoveRecordState removeRecordState,
        ITelegramBotClient client,
        UserContext userContext,
        AwaitingDateState awaitingDateState,
        AwaitingRemoveRemindState awaitingRemoveRemindState,
        ViewAllRecordsState viewAllRecordsState,
        AddRemindState addRemindState,
        ViewAllRemindersState viewAllRemindersState,
        AwaitingRemoveRecordState awaitingRemoveRecordState,
        AwaitingContentState awaitingContentState,
        RemoveRemindState removeRemindState)
    {
        _client = client;
        _userContext = userContext;
        _addRemindState = addRemindState;

        _states = new Dictionary<UserStatus, IState>
        {
            [UserStatus.AwaitingContent] = awaitingContentState,
            [UserStatus.AwaitingDate] = awaitingDateState,
            [UserStatus.AwaitingTime] = addRecordState,
            [UserStatus.AwaitingGetAllRecords] = viewAllRecordsState,
            [UserStatus.AwaitingRemoveRecord] = awaitingRemoveRecordState,
            [UserStatus.AwaitingRemoveChoice] = removeRecordState,
            [UserStatus.AwaitingRemind] = addRemindState,
            [UserStatus.AwaitingRemoveRemind] = awaitingRemoveRemindState,
            [UserStatus.AwaitingRemoveChoiceRemind] = removeRemindState,
            [UserStatus.AwaitingGetAllReminds] = viewAllRemindersState
        };
    }

    public async Task HandleState(User user, long chatId, CancellationToken cancellationToken, string? dataHandler = null)
    {
        if (user.CurrentStatus == UserStatus.AwaitingOffsetRemind)
        {
            if (int.TryParse(dataHandler, out var offsetTime))
            {
                await _addRemindState.HandleRemindOffset(user, chatId, offsetTime, cancellationToken);
                await _userContext.UpdateUserAsync(user);  
            }
            else
            {
                await _client.SendMessage(chatId, "Неверное значение смещения.", cancellationToken: cancellationToken);
            }
            return;
        }
        if (_states.TryGetValue(user.CurrentStatus, out var state))
        {
            if (dataHandler != null) 
                await state.Handle(user, chatId, cancellationToken, dataHandler);
            else
            {
                await state.Handle(user, chatId, cancellationToken);
            }
            await _userContext.UpdateUserAsync(user);
        }
        else
        {
            await _client.SendMessage(chatId, "Неизвестное состояние. Возвращаюсь в главное меню.", cancellationToken: cancellationToken);
            SetUserStatusAsync(user, UserStatus.None);
        }
    }

    public async Task SetUserStatusAsync(User user, UserStatus status)
    {
        user.CurrentStatus = status;
        await _userContext.UpdateUserAsync(user);
    }
    
}
