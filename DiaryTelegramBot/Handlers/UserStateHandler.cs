    using DiaryTelegramBot.Data;
    using DiaryTelegramBot.Handlers;
    using DiaryTelegramBot.Keyboards;
    using DiaryTelegramBot.Models;
    using DiaryTelegramBot.States;
    using Telegram.Bot;

    public class UserStateHandler
    {
        private readonly Dictionary<UserStatus, IState> _states;
        private readonly ITelegramBotClient _client;
        private readonly UserContext _userContext;
        

        public UserStateHandler(
            AwaitingAddRecordState awaitingAddRecordState,
            AwaitingRemoveRecordState awaitingRemoveRecordState,
            ITelegramBotClient client,
            UserContext userContext,
            AwaitingDateState awaitingDateState,
            AwaitingTimeState awaitingTimeState,
            AwaitingRemoveRemindState awaitingRemoveRemindState,
            ViewAllRecordsState viewAllRecordsState,
            AwaitingMainMenuState awaitingMainMenuState,
            AddRemindState addRemindState,
            ViewAllRemindersState viewAllRemindersState,
            AwaitingDeleteRecordSelection awaitingDeleteRecordSelection,
            AwaitingContentState awaitingContentState,
            RemoveRemindState removeRemindState)
        {
            _client = client;
            _userContext = userContext;

            _states = new Dictionary<UserStatus, IState>
            {
                [UserStatus.None] = awaitingMainMenuState,
                [UserStatus.AwaitingContent] = awaitingContentState,
                [UserStatus.AwaitingDate] = awaitingDateState,
                [UserStatus.AwaitingTime] = awaitingTimeState,
                [UserStatus.AwaitingAddRecord] = awaitingAddRecordState,
                [UserStatus.AwaitingGetAllRecords] = viewAllRecordsState,
                [UserStatus.AwaitingRemoveRecord] = awaitingDeleteRecordSelection,
                [UserStatus.AwaitingRemoveSelectedRecord] = awaitingRemoveRecordState,
                [UserStatus.AwaitingRemind] = addRemindState,
                [UserStatus.AwaitingRemoveRemind] = awaitingRemoveRemindState,
                [UserStatus.AwaitingRemoveChoiceRemind] = removeRemindState,
                [UserStatus.AwaitingGetAllReminds] = viewAllRemindersState
            };
        }
        

        public async Task HandleState(User user, long chatId, CancellationToken cancellationToken, string? dataHandler = null)
        {
            if (!_states.TryGetValue(user.CurrentStatus, out var state))
            {
                await _client.SendMessage(chatId, "Неизвестное состояние. Возвращаюсь в главное меню.", cancellationToken: cancellationToken);
                await SetUserStatusAsync(user, UserStatus.None);
                return;
            }
            var previousStatus = user.CurrentStatus;

            if (dataHandler != null)
                await state.Handle(user, chatId, cancellationToken, dataHandler);
            else
                await state.Handle(user, chatId, cancellationToken);

            await _userContext.UpdateUserAsync(user);

            if (user.CurrentStatus != previousStatus)
            {
                await HandleState(user, chatId, cancellationToken, null);
            }
        }
        
        public async Task SetUserStatusAsync(User user, UserStatus status)
        {
            user.CurrentStatus = status;
            await _userContext.UpdateUserAsync(user);
        }
    }
