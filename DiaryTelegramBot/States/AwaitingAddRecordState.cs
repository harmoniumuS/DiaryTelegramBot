using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AwaitingAddRecordState : IState
{
    private readonly UserStateHandler _userStateHandler;
    private readonly UserContext _userContext;
    private ITelegramBotClient _botClient;

    public AwaitingAddRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(User user,long chatId,CancellationToken cancellationToken,string dataHandler = null)
    {
        if (user.TempRecord.SentTime !=null && user.TempRecord.Text !=null)
        {
            await _userContext.AddMessageAsync(user, user.TempRecord.Text,user.TempRecord.SentTime);
            await _botClient.SendMessage(
                chatId,
                $"Запись сохранена на {user.TempRecord.SentTime:dd.MM.yyyy HH:mm}");
            user.CurrentStatus = UserStatus.None;
            user.TempRecord = null;
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                "Некорректное время. Пожалуйста, используйте формат ЧЧ:ММ:",
                cancellationToken: cancellationToken);
        }
        
    }
    
}