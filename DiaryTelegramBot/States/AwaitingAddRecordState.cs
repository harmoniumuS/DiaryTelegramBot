using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.States;
using Telegram.Bot;

public class AwaitingAddRecordState : IState
{
    private readonly UserContext _userContext;
    private readonly ITelegramBotClient _botClient;

    public AwaitingAddRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext, string data = null)
    {
        if (stateContext.TempRecord != null)
        {
            if (string.IsNullOrWhiteSpace(stateContext.TempRecord.Text))
            {
                await _botClient.SendMessage(stateContext.ChatId, "Текст записи не может быть пустым. Пожалуйста, введите текст.");
                return; 
            }

            await _userContext.AddMessageAsync(stateContext.User, stateContext.TempRecord.Text,
                stateContext.TempRecord.SentTime);
            await _botClient.EditMessageText(
                stateContext.ChatId, 
                stateContext.CallBackQueryId,
                $"Запись сохранена на дату и время: {stateContext.TempRecord.SentTime:dd.MM.yyyy HH:mm}.",
                cancellationToken: stateContext.CancellationToken);
            Task.Delay(2000).Wait();
            _botClient.DeleteMessage(
                stateContext.ChatId,
                stateContext.CallBackQueryId,
                stateContext.CancellationToken);
            BotKeyboardManager.SendMainKeyboardAsync(_botClient, stateContext);
            
            stateContext.User.CurrentStatus = UserStatus.None;
            stateContext.TempRecord = null;
            stateContext.MessageText = null;

            await _userContext.UpdateUserAsync(stateContext.User);
            
        }
        else
        {
            await _botClient.SendMessage(stateContext.ChatId, "Нет активной записи. Пожалуйста, начните добавление заново.");
        }
    }
}