using DiaryTelegramBot.Attributes;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using Telegram.Bot;

namespace DiaryTelegramBot.Handlers;
public class AwaitingAddRecordState : IState
{
    private readonly UserContext _userContext;
    private ITelegramBotClient _botClient;

    public AwaitingAddRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(StateContext stateContext,string data = null)
    {
        if (stateContext.User.TempRecord != null)
        {
            if (string.IsNullOrWhiteSpace(stateContext.User.TempRecord.Text))
            {
                await _botClient.SendMessage(stateContext.ChatId, "Текст записи не может быть пустым. Пожалуйста, введите текст.");
                return;
            }

            await _userContext.AddMessageAsync(stateContext.User, stateContext.User.TempRecord.Text,
                stateContext.User.TempRecord.SentTime);
            await _botClient.SendMessage(
                stateContext.ChatId,
                $"Запись сохранена на {stateContext.User.TempRecord.SentTime:dd.MM.yyyy HH:mm}");
            stateContext.User.CurrentStatus = UserStatus.None;
        }
        stateContext.User.TempRecord = null;
    }
    
}