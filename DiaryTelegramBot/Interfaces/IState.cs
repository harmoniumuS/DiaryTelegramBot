using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Models;

namespace DiaryTelegramBot.States;

public interface IState
{
    public Task Handle(StateContext stateContext,string data = null);
}