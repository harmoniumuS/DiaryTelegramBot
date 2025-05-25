using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

public class UserStateHandler(
    AddRecordState addRecordState,
    RemoveRecordState removeRecordState,
    ITelegramBotClient client,
    UserContext userContext,
    ViewAllRecordsState viewAllRecordsState,
    AddRemindState addRemindState,
    ViewAllRemindersState viewAllRemindersState,
    RemoveRemindState removeRemindState)
{
    private Dictionary<long, UserStatus> _states = new();

    public UserStatus GetState(User user)
    {
        if (!_states.ContainsKey(user.Id))
            _states[user.Id] = UserStatus.None; 

        return _states[user.Id];
    }
    public void SetState(long id,UserStatus state)
    {
        _states[id] = state;
    }
   
    public async Task HandleState(User user,long chatId,CancellationToken cancellationToken,string dataHandler=null)
    {
        switch (user.CurrentStatus)
        {
            case UserStatus.AwaitingContent:
                await AwaitingContentStateHandle(chatId, client);
                break;
            case UserStatus.AwaitingDate:
                await AwaitingDateStateHandle(dataHandler,user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingTime:
                await addRecordState.Handle(user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingGetAllRecords:
                await viewAllRecordsState.Handle(user, chatId, cancellationToken);
                    break;
            case UserStatus.AwaitingRemoveRecord:
                await AwaitingRemoveRecordHandle(user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingRemoveChoice:
                await removeRecordState.Handle(user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingRemind:
                await addRemindState.Handle(user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingOffsetRemind:
                var offsetTime = int.Parse(dataHandler);
                await addRemindState.HandleRemindOffset(user,chatId,offsetTime,cancellationToken);
                break;
            case UserStatus.AwaitingRemoveRemind:
                await AwaitingRemoveRemindHandle(user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingRemoveChoiceRemind:
                await removeRemindState.Handle(user,chatId,cancellationToken);
                break;
            case UserStatus.AwaitingGetAllReminds:
                await viewAllRemindersState.Handle(user,chatId,cancellationToken);
                break;
            
        }
    }

    private async Task AwaitingRemoveRecordHandle(User user, long chatId, CancellationToken cancellationToken)
    {
        var messages = await userContext.GetMessagesAsync(user.Id);

        if (!messages.Any())
        {
            await client.SendMessage(chatId, "У вас нет записей для удаления.", cancellationToken: cancellationToken);
            SetState(user.Id, UserStatus.None);
            return;
        }
        var formattedRecords = messages
            .Select((record, index) => $"{index + 1}. {record.SentTime:yyyy-MM-dd HH:mm}: {record.Text}")
            .ToList();
        
        SetState(user.Id, UserStatus.AwaitingRemoveChoice);
        
        await BotKeyboardManager.SendRemoveKeyboardAsync(client, chatId, formattedRecords, cancellationToken);
    }
    private async Task AwaitingRemoveRemindHandle(User user, long chatId, CancellationToken cancellationToken)
    {
        var messages = await userContext.GetMessagesAsync(user.Id);

        if (!messages.Any())
        {
            await client.SendMessage(chatId, "У вас нет записей для удаления.", cancellationToken: cancellationToken);
            SetState(user.Id, UserStatus.None);
            return;
        }
        var formattedRecords = messages
            .Select((record, index) => $"{index + 1}. {record.SentTime:yyyy-MM-dd HH:mm}: {record.Text}")
            .ToList();
        
        SetState(user.Id, UserStatus.AwaitingRemoveChoice);
        
        await BotKeyboardManager.SendRemoveKeyboardAsync(client, chatId, formattedRecords, cancellationToken);
    }

    private async Task AwaitingContentStateHandle(long chatId, ITelegramBotClient botClient)
    {
        await client.SendMessage(
            chatId,
            "Введите запись:",
            replyMarkup: new[]
            {
                InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "return_main_menu"),
            });
    }

    private async Task AwaitingDateStateHandle(string dateHandler, User user, long chatId , CancellationToken cancellationToken)
    {   
        var datePart = dateHandler.Substring("date:".Length);
        if (dateHandler.StartsWith("date:"))
        {
            if (DateTime.TryParse(datePart, out var parsedDate))
            {
                if (user.CurrentStatus == UserStatus.AwaitingDate)
                {
                    user.TempRecord.SentTime= parsedDate;
                    user.CurrentStatus = UserStatus.AwaitingTime;
                                    
                    await client.SendMessage(
                        chatId,
                        $"Вы выбрали дату: {parsedDate:dd.MM.yyyy}. Теперь введите время (в формате ЧЧ:ММ):",
                        cancellationToken: cancellationToken);
                    user.CurrentStatus = UserStatus.AwaitingTime;
                }
                else
                {
                    await client.SendMessage(
                        chatId,
                        "Неверное состояние. Попробуйте ещё раз.",
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                await client.SendMessage(
                    chatId,
                    "Некорректная дата, попробуйте ещё раз.",
                    cancellationToken: cancellationToken);
            }
        }
        else if (dateHandler.StartsWith("calendar:prev:") || dateHandler.StartsWith("calendar:next:"))
        {
            var action = dateHandler.Split(':')[0] == "calendar" ? dateHandler.Split(':')[1] : string.Empty;
            var partOfDate = dateHandler.Substring($"calendar:{action}:".Length);
            if (DateTime.TryParse(partOfDate, out var changeMonthDate))
            {
                var newDate = action == "prev" 
                    ? changeMonthDate.AddMonths(-1)
                    : changeMonthDate.AddMonths(1); 
                var calendarMarkup = BotKeyboardManager.CreateCalendarMarkUp(newDate);
                await client.SendMessage(
                    chatId,
                    $"Вы перешли к {newDate:MMMM yyyy}.",
                    replyMarkup: calendarMarkup,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await client.SendMessage(
                    chatId,
                    "Некорректная дата для перехода, попробуйте ещё раз.",
                    cancellationToken: cancellationToken);
            }
        }
    }

}

