using DiaryTelegramBot.Data;
using DiaryTelegramBot.Keyboards;
using DiaryTelegramBot.Models;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace DiaryTelegramBot.Handlers;

public class AddRecordState : IState
{
    private readonly UserStateHandler _userStateHandler;
    private readonly UserContext _userContext;
    private ITelegramBotClient _botClient;

    public AddRecordState(UserContext userContext, ITelegramBotClient botClient)
    {
        _userContext = userContext;
        _botClient = botClient;
    }

    public async Task Handle(User user,long chatId,CancellationToken cancellationToken,string dataHandler = null)
    {
        if (user.TempRecord.SentTime !=null)
        {
            await _userContext.AddMessageAsync(user, user.TempRecord.Text,user.TempRecord.SentTime);
            
            
            await _botClient.SendMessage(
                chatId,
                $"Запись сохранена на {user.TempRecord.SentTime:dd.MM.yyyy HH:mm}");

            await BotKeyboardManager.SendMainKeyboardAsync(_botClient, chatId, cancellationToken);
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