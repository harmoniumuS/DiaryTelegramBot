using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Models;

namespace DiaryTelegramBot.States;

public interface IState
{
    public Task Handle(User user,long chatId,CancellationToken cancellationToken,string dataHandler = null);
}