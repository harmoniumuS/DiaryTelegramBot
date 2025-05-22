using DiaryTelegramBot.Handlers;

namespace DiaryTelegramBot.States;

public interface IState
{
    public Task Handle(UserStateHandler service,long userId,long chatId,CancellationToken cancellationToken);
}