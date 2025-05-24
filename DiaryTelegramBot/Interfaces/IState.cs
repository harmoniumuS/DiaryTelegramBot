using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Models;

namespace DiaryTelegramBot.States;

public interface IState
{
    public Task Handle(UserStateHandler service,User user,CancellationToken cancellationToken);
}