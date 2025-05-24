using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Models;

namespace DiaryTelegramBot.States;

public interface IState
{
    public Task Handle(UserStateHandler handler,User user,long chatId,CancellationToken cancellationToken);
}