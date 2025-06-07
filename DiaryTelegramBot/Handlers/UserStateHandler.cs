using System.Reflection;
using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
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
            IEnumerable<IState> allStates,
            ITelegramBotClient botClient,
            UserContext userContext,
            Dictionary<UserStatus, IState> statesByStatus) 
        {
            _botClient = botClient;
            _userContext = userContext;
            _statesByStatus = statesByStatus;

            _statesByCallbackCommand = allStates
                .SelectMany(state => state.GetType()
                    .GetCustomAttributes<TelegramCallbackCommandAttribute>()
                    .Where(attr => !string.IsNullOrWhiteSpace(attr.Command))
                    .Select(attr => (attr.Command, State: state)))
                .ToDictionary(x => x.Command, x => x.State);
        }

        public async Task HandleState(StateContext context)
        {
            var user = context.User;
            var callbackData = context.CallbackData;

            if (!string.IsNullOrEmpty(callbackData))
            {
                var (prefix, state) = _statesByCallbackCommand
                    .FirstOrDefault(kvp => callbackData.StartsWith(kvp.Key));

                if (state != null)
                {
                    var data = callbackData.Substring(prefix.Length);

                    var attr = state.GetType()
                        .GetCustomAttributes<TelegramCallbackCommandAttribute>()
                        .FirstOrDefault(a => a.Command == prefix);

                    if (attr != null && attr.InitialStatus != UserStatus.NoStatus && user.CurrentStatus != attr.InitialStatus)
                    {
                        user.CurrentStatus = attr.InitialStatus;
                        await _userContext.UpdateUserAsync(user);
                    }

                    await state.Handle(context, data);
                    await _userContext.UpdateUserAsync(user);
                    return;
                }
            }

            if (!_statesByStatus.TryGetValue(user.CurrentStatus, out var currentState))
            {
                await _botClient.SendMessage(context.ChatId, "Неизвестное состояние. Возвращаюсь в главное меню.", cancellationToken: context.CancellationToken);
                user.CurrentStatus = UserStatus.None;
                await _userContext.UpdateUserAsync(user);
                return;
            }

            await currentState.Handle(context);
            await _userContext.UpdateUserAsync(user);
        }
    }
}
