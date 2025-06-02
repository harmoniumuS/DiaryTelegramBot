using System.Reflection;
using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers
{
    public class UserStateHandler
    {
        private readonly Dictionary<UserStatus, IState> _statesByStatus;
        private readonly Dictionary<string, IState> _statesByCallbackCommand;
        private readonly ITelegramBotClient _botClient;
        private readonly UserContext _userContext;

        public UserStateHandler(
            AwaitingAddRecordState awaitingAddRecordState,
            AwaitingRemoveRecordState awaitingRemoveRecordState,
            ITelegramBotClient botClient,
            UserContext userContext,
            AwaitingDateState awaitingDateState,
            AwaitingTimeState awaitingTimeState,
            AwaitingRemoveRemindState awaitingRemoveRemindState,
            AwaitingViewAllRecordsState awaitingViewAllRecordsState,
            AwaitingMainMenuState awaitingMainMenuState,
            AwaitingAddRemindState awaitingAddRemindState,
            AwaitingViewAllRemindersState awaitingViewAllRemindersState,
            AwaitingDeleteRecordSelection awaitingDeleteRecordSelection,
            AwaitingContentState awaitingContentState,
            AwaitingSelectedRemoveRemind awaitingSelectedRemoveRemind)
        {
            _botClient = botClient;
            _userContext = userContext;

            _statesByStatus = new Dictionary<UserStatus, IState>
            {
                [UserStatus.None] = awaitingMainMenuState,
                [UserStatus.AwaitingContent] = awaitingContentState,
                [UserStatus.AwaitingDate] = awaitingDateState,
                [UserStatus.AwaitingTime] = awaitingTimeState,
                [UserStatus.AwaitingAddRecord] = awaitingAddRecordState,
                [UserStatus.AwaitingGetAllRecords] = awaitingViewAllRecordsState,
                [UserStatus.AwaitingRemoveRecord] = awaitingDeleteRecordSelection,
                [UserStatus.AwaitingRemoveSelectedRecord] = awaitingRemoveRecordState,
                [UserStatus.AwaitingRemind] = awaitingAddRemindState,
                [UserStatus.AwaitingRemoveRemind] = awaitingRemoveRemindState,
                [UserStatus.AwaitingRemoveChoiceRemind] = awaitingSelectedRemoveRemind,
                [UserStatus.AwaitingGetAllReminds] = awaitingViewAllRemindersState
            };

            _statesByCallbackCommand = new Dictionary<string, IState>();

            foreach (var state in _statesByStatus.Values.Distinct())
            {
                var attrs = state.GetType().GetCustomAttributes<TelegramCallbackCommandAttribute>();
                var attr = attrs.FirstOrDefault();

                if (attr != null && !string.IsNullOrWhiteSpace(attr.Command))
                {
                    _statesByCallbackCommand[attr.Command] = state;
                }
            }

        }
       public async Task HandleState(StateContext context)
        {
            var user = context.User;
            var chatId = context.ChatId;
            var cancellationToken = context.CancellationToken;
            var callbackData = context.CallbackData;
            var messageText = context.MessageText;

            if (!string.IsNullOrEmpty(callbackData))
            {
                var targetState = _statesByCallbackCommand
                    .FirstOrDefault(kvp => callbackData.StartsWith(kvp.Key))
                    .Value;

                if (targetState != null)
                {
                    var attrs = targetState.GetType().GetCustomAttributes<TelegramCallbackCommandAttribute>();

                    foreach (var attr in attrs)
                    {
                        if (attr != null && attr.InitialStatus != UserStatus.NoStatus && user.CurrentStatus != attr.InitialStatus)
                        {
                            await SetUserStatusAsync(user, attr.InitialStatus);
                            break;
                        }
                    }

                    await HandleStateMethodAsync(targetState, context);
                    await _userContext.UpdateUserAsync(user);
                    return;
                }
            }

            if (!_statesByStatus.TryGetValue(user.CurrentStatus, out var currentState))
            {
                await _botClient.SendMessage(chatId, "Неизвестное состояние. Возвращаюсь в главное меню.", cancellationToken: cancellationToken);
                await SetUserStatusAsync(user, UserStatus.None);
                return;
            }

            var previousStatus = user.CurrentStatus;

            if (!string.IsNullOrEmpty(callbackData))
            {
                await HandleStateMethodAsync(currentState, context, callbackData);
            }
            else if (!string.IsNullOrEmpty(messageText))
            {
                await HandleStateMethodAsync(currentState, context, messageText);
            }
            else
            {
                await HandleStateMethodAsync(currentState, context);
            }

            await _userContext.UpdateUserAsync(user);

            if (user.CurrentStatus != previousStatus)
            {
                context.CallbackData = null;
                await HandleState(context);
            }
        }
        private async Task HandleStateMethodAsync(IState state, StateContext context, string? data = null)
        {
            if (data != null)
            {
                await state.Handle(context, data);
            }
            else
            {
                await state.Handle(context);
            }
        }
        public async Task SetUserStatusAsync(User user, UserStatus status)
        {
            user.CurrentStatus = status;
            await _userContext.UpdateUserAsync(user);
        }
    }
}
